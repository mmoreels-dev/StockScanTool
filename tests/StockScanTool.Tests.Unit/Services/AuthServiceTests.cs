using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Services;

namespace StockScanTool.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IScanningDeviceRepository> _deviceRepoMock;
    private readonly Mock<IRepository<User>> _userRepoMock;
    private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJwtTokenService> _jwtMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _sut;
    private readonly IApiKeyHasher _hasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOptions<JwtSettings> _jwtSettings;

    public AuthServiceTests()
    {
        _hasher = new ApiKeyHasher("test-pepper");
        _passwordHasher = new PasswordHasher();
        _jwtSettings = Microsoft.Extensions.Options.Options.Create(new JwtSettings { RefreshTokenExpirationDays = 7 });
        _deviceRepoMock = new Mock<IScanningDeviceRepository>();
        _userRepoMock = new Mock<IRepository<User>>();
        _refreshTokenRepoMock = new Mock<IRepository<RefreshToken>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtMock = new Mock<IJwtTokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _sut = new AuthService(_deviceRepoMock.Object, _userRepoMock.Object, _refreshTokenRepoMock.Object, _unitOfWorkMock.Object, _jwtMock.Object, _hasher, _passwordHasher, _jwtSettings, _loggerMock.Object);
    }

    [Fact]
    public async Task LoginDeviceAsync_ReturnsToken_WhenValidApiKey()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        var device = new ScanningDevice
        {
            Id = 1,
            DeviceName = "Scanner 1",
            StoreId = 1,
            ApiKey = _hasher.Hash("valid-key"),
            IsActive = true,
            Store = store
        };
        _deviceRepoMock.Setup(r => r.GetByApiKeyAsync(_hasher.Hash("valid-key"))).ReturnsAsync(device);
        _jwtMock.Setup(j => j.GenerateDeviceToken(1, 1)).Returns("jwt-token-123");

        var result = await _sut.LoginDeviceAsync(new DeviceLoginRequest("valid-key"));

        result.Should().NotBeNull();
        result!.DeviceId.Should().Be(1);
        result.DeviceName.Should().Be("Scanner 1");
        result.StoreId.Should().Be(1);
        result.StoreName.Should().Be("Main Store");
        result.Token.Should().Be("jwt-token-123");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginDeviceAsync_ReturnsNull_WhenInvalidApiKey()
    {
        _deviceRepoMock.Setup(r => r.GetByApiKeyAsync(It.IsAny<string>())).ReturnsAsync((ScanningDevice?)null);

        var result = await _sut.LoginDeviceAsync(new DeviceLoginRequest("bad-key"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginDeviceAsync_ReturnsNull_WhenDeviceInactive()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        var device = new ScanningDevice
        {
            Id = 1,
            DeviceName = "Scanner 1",
            StoreId = 1,
            ApiKey = _hasher.Hash("inactive-key"),
            IsActive = false,
            Store = store
        };
        _deviceRepoMock.Setup(r => r.GetByApiKeyAsync(_hasher.Hash("inactive-key"))).ReturnsAsync(device);

        var result = await _sut.LoginDeviceAsync(new DeviceLoginRequest("inactive-key"));

        result.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginDeviceAsync_UpdatesLastPing()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        var device = new ScanningDevice
        {
            Id = 1,
            DeviceName = "Scanner 1",
            StoreId = 1,
            ApiKey = _hasher.Hash("valid-key"),
            IsActive = true,
            Store = store,
            LastPing = null
        };
        _deviceRepoMock.Setup(r => r.GetByApiKeyAsync(_hasher.Hash("valid-key"))).ReturnsAsync(device);
        _jwtMock.Setup(j => j.GenerateDeviceToken(1, 1)).Returns("token");

        await _sut.LoginDeviceAsync(new DeviceLoginRequest("valid-key"));

        device.LastPing.Should().NotBeNull();
        device.LastPing.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LoginDeviceAsync_HashesApiKeyBeforeLookup()
    {
        _deviceRepoMock.Setup(r => r.GetByApiKeyAsync(It.IsAny<string>())).ReturnsAsync((ScanningDevice?)null);

        await _sut.LoginDeviceAsync(new DeviceLoginRequest("test-api-key"));

        _deviceRepoMock.Verify(r => r.GetByApiKeyAsync(_hasher.Hash("test-api-key")), Times.Once);
    }

    [Fact]
    public async Task LoginUserAsync_ReturnsToken_WhenValidCredentials()
    {
        var permission = new Permission { Id = 1, Code = "dashboard.read", Name = "View Dashboard", GroupName = "Dashboard" };
        var role = new Role
        {
            Id = 1,
            Name = "Admin",
            Description = "Full access",
            RolePermissions = [new RolePermission { RoleId = 1, PermissionId = 1, Permission = permission }]
        };
        var user = new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = _passwordHasher.Hash("admin"),
            DisplayName = "Admin User",
            IsActive = true,
            UserRoles = [new UserRole { UserId = 1, RoleId = 1, Role = role }]
        };

        var users = new List<User> { user };
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(users.BuildMock());
        _jwtMock.Setup(j => j.GenerateUserToken(1, "admin", "Admin User",
            It.Is<List<string>>(l => l.Contains("Admin")),
            It.Is<List<string>>(l => l.Contains("dashboard.read"))))
            .Returns("user-jwt-token");

        var result = await _sut.LoginUserAsync("admin", "admin");

        result.Should().NotBeNull();
        result!.Token.Should().Be("user-jwt-token");
    }

    [Fact]
    public async Task LoginUserAsync_ReturnsNull_WhenUserNotFound()
    {
        var users = new List<User>();
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(users.BuildMock());

        var result = await _sut.LoginUserAsync("nonexistent", "password");

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserAsync_ReturnsNull_WhenUserInactive()
    {
        var user = new User
        {
            Id = 1,
            Username = "inactive",
            PasswordHash = _passwordHasher.Hash("pass"),
            DisplayName = "Inactive",
            IsActive = false,
            UserRoles = []
        };

        var users = new List<User> { user };
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(users.BuildMock());

        var result = await _sut.LoginUserAsync("inactive", "pass");

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserAsync_ReturnsNull_WhenWrongPassword()
    {
        var user = new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = _passwordHasher.Hash("correct-password"),
            DisplayName = "Admin",
            IsActive = true,
            UserRoles = []
        };

        var users = new List<User> { user };
        _userRepoMock.Setup(r => r.AsQueryable()).Returns(users.BuildMock());

        var result = await _sut.LoginUserAsync("admin", "wrong-password");

        result.Should().BeNull();
    }
}
