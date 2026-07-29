using Xunit;
using Moq;
using FluentAssertions;
using StockScanTool.Infrastructure.Services;
using StockScanTool.Application.Repositories;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;
using FluentValidation;

namespace StockScanTool.Tests.Unit.Services;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IValidator<UpdateInventoryRequest>> _validatorMock;
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _inventoryRepoMock = new Mock<IInventoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _validatorMock = new Mock<IValidator<UpdateInventoryRequest>>();
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateInventoryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _sut = new InventoryService(_inventoryRepoMock.Object, _unitOfWorkMock.Object, _validatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var product = new Product { Id = 1, Name = "Widget", Barcode = "123456" };
        var store = new Store { Id = 1, Name = "Main Store" };
        var inventories = new List<Inventory>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 50, Product = product, Store = store }
        };
        _inventoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(inventories);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].ProductName.Should().Be("Widget");
        result[0].QuantityOnHand.Should().Be(50);
    }

    [Fact]
    public async Task GetByStoreAsync_ReturnsFilteredDtos()
    {
        var product = new Product { Id = 1, Name = "Widget", Barcode = "123456" };
        var store = new Store { Id = 1, Name = "Main Store" };
        var inventories = new List<Inventory>
        {
            new() { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 50, Product = product, Store = store }
        };
        _inventoryRepoMock.Setup(r => r.GetByStoreAsync(1)).ReturnsAsync(inventories);

        var result = await _sut.GetByStoreAsync(1);

        result.Should().HaveCount(1);
        result[0].StoreId.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        var product = new Product { Id = 1, Name = "Widget", Barcode = "123456" };
        var store = new Store { Id = 1, Name = "Main Store" };
        var inventory = new Inventory { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 50, Product = product, Store = store };
        _inventoryRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inventory);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.QuantityOnHand.Should().Be(50);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _inventoryRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Inventory?)null);

        var result = await _sut.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_CreatesNew_WhenNotExist()
    {
        var product = new Product { Id = 1, Name = "Widget", Barcode = "123456" };
        var store = new Store { Id = 1, Name = "Main Store" };
        var saved = new Inventory { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 25, Product = product, Store = store };

        _inventoryRepoMock.SetupSequence(r => r.GetByProductAndStoreAsync(1, 1))
            .ReturnsAsync((Inventory?)null)
            .ReturnsAsync(saved);
        _inventoryRepoMock.Setup(r => r.AddAsync(It.IsAny<Inventory>()))
            .Callback<Inventory>(i => i.Id = 1)
            .ReturnsAsync((Inventory i) => i);

        var result = await _sut.UpsertAsync(new UpdateInventoryRequest(1, 1, 25));

        result.ProductId.Should().Be(1);
        result.QuantityOnHand.Should().Be(25);
        _inventoryRepoMock.Verify(r => r.AddAsync(It.IsAny<Inventory>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_UpdatesExisting_WhenExists()
    {
        var product = new Product { Id = 1, Name = "Widget", Barcode = "123456" };
        var store = new Store { Id = 1, Name = "Main Store" };
        var existing = new Inventory { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 10, Product = product, Store = store };
        var updated = new Inventory { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 30, Product = product, Store = store };

        _inventoryRepoMock.SetupSequence(r => r.GetByProductAndStoreAsync(1, 1))
            .ReturnsAsync(existing)
            .ReturnsAsync(updated);

        var result = await _sut.UpsertAsync(new UpdateInventoryRequest(1, 1, 30));

        result.QuantityOnHand.Should().Be(30);
        _inventoryRepoMock.Verify(r => r.AddAsync(It.IsAny<Inventory>()), Times.Never);
        _inventoryRepoMock.Verify(r => r.Update(existing), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DecrementStockAsync_ReturnsTrue_WhenSufficientStock()
    {
        var inventory = new Inventory { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 10 };
        _inventoryRepoMock.Setup(r => r.GetByProductAndStoreAsync(1, 1)).ReturnsAsync(inventory);

        var result = await _sut.DecrementStockAsync(1, 1, 3);

        result.Should().BeTrue();
        inventory.QuantityOnHand.Should().Be(7);
        _inventoryRepoMock.Verify(r => r.Update(inventory), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DecrementStockAsync_ReturnsFalse_WhenInsufficientStock()
    {
        var inventory = new Inventory { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 2 };
        _inventoryRepoMock.Setup(r => r.GetByProductAndStoreAsync(1, 1)).ReturnsAsync(inventory);

        var result = await _sut.DecrementStockAsync(1, 1, 5);

        result.Should().BeFalse();
        inventory.QuantityOnHand.Should().Be(2);
        _inventoryRepoMock.Verify(r => r.Update(It.IsAny<Inventory>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DecrementStockAsync_ReturnsFalse_WhenInventoryNotFound()
    {
        _inventoryRepoMock.Setup(r => r.GetByProductAndStoreAsync(1, 1)).ReturnsAsync((Inventory?)null);

        var result = await _sut.DecrementStockAsync(1, 1, 1);

        result.Should().BeFalse();
    }
}
