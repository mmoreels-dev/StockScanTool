using Xunit;
using Moq;
using FluentAssertions;
using FluentValidation;
using MockQueryable;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Services;

namespace StockScanTool.Tests.Unit.Services;

public class RoleServiceTests
{
    private readonly Mock<IRepository<Role>> _roleRepoMock;
    private readonly Mock<IRepository<Permission>> _permissionRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IValidator<CreateRoleRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateRoleRequest>> _updateValidatorMock;
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _roleRepoMock = new Mock<IRepository<Role>>();
        _permissionRepoMock = new Mock<IRepository<Permission>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _createValidatorMock = new Mock<IValidator<CreateRoleRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateRoleRequest>>();
        _sut = new RoleService(_roleRepoMock.Object, _permissionRepoMock.Object, _unitOfWorkMock.Object,
            _createValidatorMock.Object, _updateValidatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRoles()
    {
        var roles = new List<Role>
        {
            new() { Id = 1, Name = "Admin", Description = "Full access", IsActive = true, RolePermissions = [] },
            new() { Id = 2, Name = "Viewer", Description = "Read only", IsActive = true, RolePermissions = [] }
        };
        _roleRepoMock.Setup(r => r.AsQueryable()).Returns(roles.BuildMock());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRole_WhenExists()
    {
        var permission = new Permission { Id = 1, Code = "dashboard.read", Name = "View Dashboard", GroupName = "Dashboard" };
        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Full access",
            IsActive = true,
            RolePermissions = [new RolePermission { RoleId = 1, PermissionId = 1, Permission = permission }]
        };
        var roles = new List<Role> { role };
        _roleRepoMock.Setup(r => r.AsQueryable()).Returns(roles.BuildMock());

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Admin");
        result.Permissions.Should().Contain("dashboard.read");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _roleRepoMock.Setup(r => r.AsQueryable()).Returns(new List<Role>().BuildMock());

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesRole_WithValidRequest()
    {
        var permission = new Permission { Id = 1, Code = "dashboard.read", Name = "View Dashboard", GroupName = "Dashboard" };
        var permissions = new List<Permission> { permission };
        _roleRepoMock.Setup(r => r.AsQueryable()).Returns(new List<Role>().BuildMock());
        _permissionRepoMock.Setup(r => r.AsQueryable()).Returns(permissions.BuildMock());
        _createValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateRoleRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var result = await _sut.CreateAsync(new CreateRoleRequest("Manager", "Operational access", [1]));

        result.Should().NotBeNull();
        result.Name.Should().Be("Manager");
        _roleRepoMock.Verify(r => r.AddAsync(It.IsAny<Role>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenNameExists()
    {
        var roles = new List<Role>
        {
            new() { Id = 1, Name = "Admin", Description = "Full access", IsActive = true, RolePermissions = [] }
        };
        _roleRepoMock.Setup(r => r.AsQueryable()).Returns(new List<Role> { new() { Id = 1, Name = "Admin", Description = "Full access", IsActive = true, RolePermissions = [] } }.BuildMock());
        _createValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateRoleRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var act = () => _sut.CreateAsync(new CreateRoleRequest("Admin", "Dup", []));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesRole_WhenExists()
    {
        var permission = new Permission { Id = 1, Code = "dashboard.read", Name = "View Dashboard", GroupName = "Dashboard" };
        var role = new Role
        {
            Id = 1,
            Name = "Old",
            Description = "Old desc",
            IsActive = true,
            RolePermissions = []
        };
        var roles = new List<Role> { role };
        _roleRepoMock.Setup(r => r.AsQueryable()).Returns(roles.BuildMock());
        _permissionRepoMock.Setup(r => r.AsQueryable()).Returns(new List<Permission> { permission }.BuildMock());
        _updateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateRoleRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var result = await _sut.UpdateAsync(1,
            new UpdateRoleRequest("Updated", "Updated desc", false, [1]));

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.Description.Should().Be("Updated desc");
        result.IsActive.Should().BeFalse();
        result.Permissions.Should().Contain("dashboard.read");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotFound()
    {
        _roleRepoMock.Setup(r => r.AsQueryable()).Returns(new List<Role>().BuildMock());
        _updateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateRoleRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var result = await _sut.UpdateAsync(999,
            new UpdateRoleRequest("Nobody", "Nope", true, []));

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenRoleExists()
    {
        var role = new Role { Id = 1, Name = "ToDelete", Description = "Delete me", IsActive = true };
        _roleRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(role);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
        _roleRepoMock.Verify(r => r.Remove(role), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _roleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Role?)null);

        var result = await _sut.DeleteAsync(999);

        result.Should().BeFalse();
    }
}
