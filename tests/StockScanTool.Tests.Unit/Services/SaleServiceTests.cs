using Xunit;
using Moq;
using FluentAssertions;
using StockScanTool.Api.Services;
using StockScanTool.Application.Repositories;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Tests.Unit.Services;

public class SaleServiceTests
{
    private readonly Mock<ISaleTransactionRepository> _saleRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<IInventoryRepository> _inventoryRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SaleService _sut;

    public SaleServiceTests()
    {
        _saleRepoMock = new Mock<ISaleTransactionRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _inventoryRepoMock = new Mock<IInventoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _sut = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _inventoryRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task SubmitSaleAsync_ReturnsFailure_WhenEmptyCart()
    {
        var request = new SubmitSaleRequest(1, 1, []);

        var result = await _sut.SubmitSaleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("empty cart");
    }

    [Fact]
    public async Task SubmitSaleAsync_ReturnsFailure_WhenProductNotFound()
    {
        var request = new SubmitSaleRequest(1, 1, [new(99, 1)]);

        _productRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(new List<Product>());

        _inventoryRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
            .ReturnsAsync(new List<Inventory>());

        var result = await _sut.SubmitSaleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task SubmitSaleAsync_ReturnsFailure_WhenInsufficientStock()
    {
        var product = new Product { Id = 1, Name = "Widget", Price = 10m, Barcode = "123", Sku = "W1" };
        var inventory = new Inventory { Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 2 };

        var request = new SubmitSaleRequest(1, 1, [new(1, 5)]);

        _productRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(new List<Product> { product });

        _inventoryRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
            .ReturnsAsync(new List<Inventory> { inventory });

        var result = await _sut.SubmitSaleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Insufficient stock"));
    }

    [Fact]
    public async Task SubmitSaleAsync_ReturnsSuccess_WhenValidSale()
    {
        var product = new Product { Id = 1, Name = "Widget", Price = 10m, Barcode = "123", Sku = "W1" };
        var inventory = new Inventory
        {
            Id = 1, ProductId = 1, StoreId = 1, QuantityOnHand = 10,
            Product = product,
            Store = new Store { Id = 1, Name = "Main Store" }
        };

        var request = new SubmitSaleRequest(1, 1, [new(1, 2)]);

        _productRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(new List<Product> { product });

        _inventoryRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Inventory, bool>>>()))
            .ReturnsAsync(new List<Inventory> { inventory });

        _saleRepoMock.Setup(r => r.AddAsync(It.IsAny<SaleTransaction>()))
            .Callback<SaleTransaction>(t =>
            {
                t.Id = 1;
                t.Store = inventory.Store;
                t.ScanningDevice = new ScanningDevice { Id = 1, DeviceName = "Scanner 1" };
                foreach (var item in t.SaleItems)
                    item.Product = product;
            })
            .ReturnsAsync((SaleTransaction t) => t);

        var result = await _sut.SubmitSaleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalAmount.Should().Be(20m);
        inventory.QuantityOnHand.Should().Be(8);
    }
}
