using Xunit;
using Moq;
using FluentAssertions;
using FluentValidation;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Services;

namespace StockScanTool.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly Mock<IRepository<Role>> _roleRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IValidator<CreateUserRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateUserRequest>> _updateValidatorMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userRepoMock = new Mock<IRepository<User>>();
        _roleRepoMock = new Mock<IRepository<Role>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _createValidatorMock = new Mock<IValidator<CreateUserRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateUserRequest>>();
        _sut = new UserService(_userRepoMock.Object, _roleRepoMock.Object, _unitOfWorkMock.Object,
            _createValidatorMock.Object, _updateValidatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        var users = new List<User>
        {
            new() { Id = 1, Username = "admin", DisplayName = "Admin", IsActive = true, UserRoles = [] },
            new() { Id = 2, Username = "manager", DisplayName = "Manager", IsActive = true, UserRoles = [] }
        };
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(users.AsAsyncQueryable());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        var role = new Role { Id = 1, Name = "Admin" };
        var user = new User
        {
            Id = 1,
            Username = "admin",
            DisplayName = "Admin",
            IsActive = true,
            UserRoles = [new UserRole { UserId = 1, RoleId = 1, Role = role }]
        };
        var users = new List<User> { user }.AsQueryable();
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(users.AsAsyncQueryable());

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
        result.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(new List<User>().AsAsyncQueryable());

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesUser_WithValidRequest()
    {
        var adminRole = new Role { Id = 1, Name = "Admin", IsActive = true };
        var roles = new List<Role> { adminRole }.AsQueryable();
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(new List<User>().AsAsyncQueryable());
        _roleRepoMock.Setup(r => r.AsQueryable()).Returns(roles.AsAsyncQueryable());
        _createValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var result = await _sut.CreateAsync(new CreateUserRequest("newuser", "password123", "New User", [1]));

        result.Should().NotBeNull();
        result.Username.Should().Be("newuser");
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenUsernameExists()
    {
        var users = new List<User>
        {
            new() { Id = 1, Username = "existing", DisplayName = "Existing", IsActive = true, UserRoles = [] }
        }.AsAsyncQueryable();
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(users.AsAsyncQueryable());
        _createValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var act = () => _sut.CreateAsync(new CreateUserRequest("existing", "pass", "Dup", [1]));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUser_WhenExists()
    {
        var adminRole = new Role { Id = 1, Name = "Admin" };
        var user = new User
        {
            Id = 1,
            Username = "old",
            DisplayName = "Old Name",
            IsActive = true,
            UserRoles = [new UserRole { UserId = 1, RoleId = 1, Role = adminRole }]
        };
        var users = new List<User> { user }.AsAsyncQueryable();
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(users.AsAsyncQueryable());
        _roleRepoMock.Setup(r => r.AsQueryable()).Returns(new List<Role> { adminRole }.AsAsyncQueryable());
        _updateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var result = await _sut.UpdateAsync(1,
            new UpdateUserRequest("updated", "Updated Name", false, [1]));

        result.Should().NotBeNull();
        result!.Username.Should().Be("updated");
        result.DisplayName.Should().Be("Updated Name");
        result.IsActive.Should().BeFalse();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotFound()
    {
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(new List<User>().AsAsyncQueryable());
        _updateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var result = await _sut.UpdateAsync(999,
            new UpdateUserRequest("nobody", "Nobody", true, []));

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenUserExists()
    {
        var user = new User { Id = 1, Username = "todelete", DisplayName = "Delete", IsActive = true };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
        _userRepoMock.Verify(r => r.Remove(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _sut.DeleteAsync(999);

        result.Should().BeFalse();
    }
}
