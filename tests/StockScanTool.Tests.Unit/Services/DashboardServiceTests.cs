using Xunit;
using Moq;
using FluentAssertions;
using MockQueryable;
using StockScanTool.Infrastructure.Services;
using StockScanTool.Application.Repositories;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Tests.Unit.Services;

public class DashboardServiceTests
{
    private readonly Mock<IRepository<Store>> _storeRepoMock;
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<ScanningDevice>> _deviceRepoMock;
    private readonly Mock<ISaleTransactionRepository> _saleRepoMock;
    private readonly Mock<IInventoryRepository> _inventoryRepoMock;
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _storeRepoMock = new Mock<IRepository<Store>>();
        _productRepoMock = new Mock<IRepository<Product>>();
        _deviceRepoMock = new Mock<IRepository<ScanningDevice>>();
        _saleRepoMock = new Mock<ISaleTransactionRepository>();
        _inventoryRepoMock = new Mock<IInventoryRepository>();
        _sut = new DashboardService(
            _storeRepoMock.Object,
            _productRepoMock.Object,
            _deviceRepoMock.Object,
            _saleRepoMock.Object,
            _inventoryRepoMock.Object);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsCorrectCounts()
    {
        _storeRepoMock.Setup(r => r.CountAsync()).ReturnsAsync(3);
        _productRepoMock.Setup(r => r.CountAsync()).ReturnsAsync(10);
        _deviceRepoMock.Setup(r => r.CountAsync()).ReturnsAsync(5);

        var store = new Store { Id = 1, Name = "Main" };
        var device = new ScanningDevice { Id = 1, DeviceName = "Scanner 1" };

        var sales = new List<SaleTransaction>
        {
            new() { Id = 1, StoreId = 1, Store = store, ScanningDevice = device, TotalAmount = 100m, SaleDate = DateTime.UtcNow, SaleItems = [] },
            new() { Id = 2, StoreId = 1, Store = store, ScanningDevice = device, TotalAmount = 50m, SaleDate = DateTime.UtcNow, SaleItems = [] }
        };
        _saleRepoMock.Setup(r => r.AsQueryable()).Returns(sales.BuildMock());

        var product = new Product { Id = 1, Name = "Widget", Barcode = "123" };
        var inventory = new List<Inventory>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 25, Product = product, Store = store }
        };
        _inventoryRepoMock.Setup(r => r.AsQueryable()).Returns(inventory.BuildMock());

        var result = await _sut.GetSummaryAsync();

        result.TotalStores.Should().Be(3);
        result.TotalProducts.Should().Be(10);
        result.TotalDevices.Should().Be(5);
        result.TotalSalesRevenue.Should().Be(150m);
        result.SalesByStore.Should().HaveCount(1);
        result.SalesByStore[0].TotalRevenue.Should().Be(150m);
        result.StockLevels.Should().HaveCount(1);
        result.StockLevels[0].QuantityOnHand.Should().Be(25);
    }
}
