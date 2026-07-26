using Xunit;
using Moq;
using FluentAssertions;
using StockScanTool.Api.Services;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IScanningDeviceRepository> _deviceRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJwtTokenService> _jwtMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _deviceRepoMock = new Mock<IScanningDeviceRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtMock = new Mock<IJwtTokenService>();
        _sut = new AuthService(_deviceRepoMock.Object, _unitOfWorkMock.Object, _jwtMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenValidApiKey()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        var device = new ScanningDevice
        {
            Id = 1,
            DeviceName = "Scanner 1",
            StoreId = 1,
            ApiKey = "valid-key",
            IsActive = true,
            Store = store
        };
        _deviceRepoMock.Setup(r => r.GetByApiKeyAsync("valid-key")).ReturnsAsync(device);
        _jwtMock.Setup(j => j.GenerateDeviceToken(1, 1)).Returns("jwt-token-123");

        var result = await _sut.LoginAsync(new DeviceLoginRequest("valid-key"));

        result.Should().NotBeNull();
        result!.DeviceId.Should().Be(1);
        result.DeviceName.Should().Be("Scanner 1");
        result.StoreId.Should().Be(1);
        result.StoreName.Should().Be("Main Store");
        result.Token.Should().Be("jwt-token-123");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenInvalidApiKey()
    {
        _deviceRepoMock.Setup(r => r.GetByApiKeyAsync("bad-key")).ReturnsAsync((ScanningDevice?)null);

        var result = await _sut.LoginAsync(new DeviceLoginRequest("bad-key"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenDeviceInactive()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        var device = new ScanningDevice
        {
            Id = 1,
            DeviceName = "Scanner 1",
            StoreId = 1,
            ApiKey = "inactive-key",
            IsActive = false,
            Store = store
        };
        _deviceRepoMock.Setup(r => r.GetByApiKeyAsync("inactive-key")).ReturnsAsync(device);

        var result = await _sut.LoginAsync(new DeviceLoginRequest("inactive-key"));

        result.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UpdatesLastPing()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        var device = new ScanningDevice
        {
            Id = 1,
            DeviceName = "Scanner 1",
            StoreId = 1,
            ApiKey = "valid-key",
            IsActive = true,
            Store = store,
            LastPing = null
        };
        _deviceRepoMock.Setup(r => r.GetByApiKeyAsync("valid-key")).ReturnsAsync(device);
        _jwtMock.Setup(j => j.GenerateDeviceToken(1, 1)).Returns("token");

        await _sut.LoginAsync(new DeviceLoginRequest("valid-key"));

        device.LastPing.Should().NotBeNull();
        device.LastPing.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
