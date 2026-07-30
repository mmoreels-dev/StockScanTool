using Xunit;
using Moq;
using FluentAssertions;
using StockScanTool.Infrastructure.Services;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;
using FluentValidation;

namespace StockScanTool.Tests.Unit.Services;

public class DeviceServiceTests
{
    private readonly Mock<IScanningDeviceRepository> _deviceRepoMock;
    private readonly Mock<IStoreRepository> _storeRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IValidator<CreateDeviceRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateDeviceRequest>> _updateValidatorMock;
    private readonly DeviceService _sut;
    private readonly IApiKeyHasher _hasher;

    public DeviceServiceTests()
    {
        _hasher = new ApiKeyHasher("test-pepper");
        _deviceRepoMock = new Mock<IScanningDeviceRepository>();
        _storeRepoMock = new Mock<IStoreRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _createValidatorMock = new Mock<IValidator<CreateDeviceRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateDeviceRequest>>();
        _createValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateDeviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _updateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateDeviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _sut = new DeviceService(_deviceRepoMock.Object, _storeRepoMock.Object, _unitOfWorkMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object, _hasher);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDevicesWithStoreNames()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        var devices = new List<ScanningDevice>
        {
            new() { Id = 1, DeviceName = "Scanner 1", StoreId = 1, ApiKey = "key1", IsActive = true },
            new() { Id = 2, DeviceName = "Scanner 2", StoreId = 1, ApiKey = "key2", IsActive = false }
        };
        _deviceRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(devices);
        _storeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);
        _storeRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Store, bool>>>()))
            .ReturnsAsync(new List<Store> { store });

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].DeviceName.Should().Be("Scanner 1");
        result[0].StoreName.Should().Be("Main Store");
        result[1].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        var device = new ScanningDevice { Id = 1, DeviceName = "Scanner 1", StoreId = 1, ApiKey = "key1", IsActive = true };
        _deviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(device);
        _storeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);
        _storeRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Store, bool>>>()))
            .ReturnsAsync(new List<Store> { store });

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.DeviceName.Should().Be("Scanner 1");
        result.StoreName.Should().Be("Main Store");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _deviceRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ScanningDevice?)null);

        var result = await _sut.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_AddsDeviceAndReturnsDto()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        _deviceRepoMock.Setup(r => r.AddAsync(It.IsAny<ScanningDevice>()))
            .Callback<ScanningDevice>(d => d.Id = 1)
            .ReturnsAsync((ScanningDevice d) => d);
        _storeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);

        var request = new CreateDeviceRequest("New Scanner", 1);

        var result = await _sut.CreateAsync(request);

        result.Id.Should().Be(1);
        result.DeviceName.Should().Be("New Scanner");
        result.StoreName.Should().Be("Main Store");
        result.ApiKey.Should().NotBeNullOrEmpty();
        result.IsActive.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDeviceAndReturnsDto()
    {
        var store = new Store { Id = 2, Name = "Second Store" };
        var existing = new ScanningDevice { Id = 1, DeviceName = "Old Name", StoreId = 1, ApiKey = "key1", IsActive = true };
        _deviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _storeRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(store);

        var request = new UpdateDeviceRequest("New Name", 2, false);

        var result = await _sut.UpdateAsync(1, request);

        result.Should().NotBeNull();
        result!.DeviceName.Should().Be("New Name");
        result.StoreId.Should().Be(2);
        result.StoreName.Should().Be("Second Store");
        result.IsActive.Should().BeFalse();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotFound()
    {
        _deviceRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ScanningDevice?)null);

        var result = await _sut.UpdateAsync(99, new UpdateDeviceRequest("X", 1, true));

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenFound()
    {
        var device = new ScanningDevice { Id = 1, DeviceName = "Scanner 1" };
        _deviceRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(device);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
        _deviceRepoMock.Verify(r => r.Remove(device), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _deviceRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ScanningDevice?)null);

        var result = await _sut.DeleteAsync(99);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_GeneratesUniqueApiKey()
    {
        var store = new Store { Id = 1, Name = "Main Store" };
        _deviceRepoMock.Setup(r => r.AddAsync(It.IsAny<ScanningDevice>()))
            .Callback<ScanningDevice>(d => d.Id = 1)
            .ReturnsAsync((ScanningDevice d) => d);
        _storeRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);

        var result1 = await _sut.CreateAsync(new CreateDeviceRequest("Scanner A", 1));
        var result2 = await _sut.CreateAsync(new CreateDeviceRequest("Scanner B", 1));

        result1.ApiKey.Should().NotBeNullOrEmpty();
        result2.ApiKey.Should().NotBeNullOrEmpty();
        result1.ApiKey.Should().NotBe(result2.ApiKey);
    }
}
