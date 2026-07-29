using Xunit;
using Moq;
using FluentAssertions;
using StockScanTool.Infrastructure.Services;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace StockScanTool.Tests.Unit.Services;

public class SaleServiceTests
{
    private readonly Mock<ISaleTransactionRepository> _saleRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<IInventoryRepository> _inventoryRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IValidator<SubmitSaleRequest>> _validatorMock;
    private readonly Mock<ILogger<SaleService>> _loggerMock;
    private readonly SaleService _sut;

    public SaleServiceTests()
    {
        _saleRepoMock = new Mock<ISaleTransactionRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _inventoryRepoMock = new Mock<IInventoryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _validatorMock = new Mock<IValidator<SubmitSaleRequest>>();
        _loggerMock = new Mock<ILogger<SaleService>>();
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<SubmitSaleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _sut = new SaleService(
            _saleRepoMock.Object,
            _productRepoMock.Object,
            _inventoryRepoMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
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

        _saleRepoMock.Setup(r => r.GetByIdWithIncludesAsync(1))
            .ReturnsAsync((int id) => new SaleTransaction
            {
                Id = id,
                StoreId = 1,
                Store = inventory.Store,
                ScanningDeviceId = 1,
                ScanningDevice = new ScanningDevice { Id = 1, DeviceName = "Scanner 1" },
                TotalAmount = 20m,
                SaleDate = DateTime.UtcNow,
                SaleItems =
                [
                    new SaleItem { Id = 1, ProductId = 1, Product = product, Quantity = 2, PriceAtTimeOfSale = product.Price }
                ]
            });

        var result = await _sut.SubmitSaleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalAmount.Should().Be(20m);
        inventory.QuantityOnHand.Should().Be(8);
    }

    [Fact]
    public async Task SubmitSaleAsync_ReturnsFailure_WhenValidationFails()
    {
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<SubmitSaleRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([
                new FluentValidation.Results.ValidationFailure("StoreId", "Store ID must be positive.")]));

        var request = new SubmitSaleRequest(0, 1, [new(1, 1)]);

        var result = await _sut.SubmitSaleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Store ID"));
    }
}
