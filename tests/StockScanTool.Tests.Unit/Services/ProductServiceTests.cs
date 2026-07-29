using Xunit;
using Moq;
using FluentAssertions;
using StockScanTool.Infrastructure.Services;
using StockScanTool.Application.Repositories;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;
using FluentValidation;

namespace StockScanTool.Tests.Unit.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IValidator<CreateProductRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateProductRequest>> _updateValidatorMock;
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _repoMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _createValidatorMock = new Mock<IValidator<CreateProductRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateProductRequest>>();
        _createValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateProductRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _updateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateProductRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _sut = new ProductService(_repoMock.Object, _unitOfWorkMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var products = new List<Product>
        {
            new() { Id = 1, Sku = "W1", Name = "Widget", Description = "A widget", Barcode = "123456", Price = 9.99m },
            new() { Id = 2, Sku = "G1", Name = "Gadget", Description = "A gadget", Barcode = "789012", Price = 19.99m }
        };
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Widget");
        result[1].Price.Should().Be(19.99m);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        var product = new Product { Id = 1, Sku = "W1", Name = "Widget", Description = "A widget", Barcode = "123456", Price = 9.99m };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Sku.Should().Be("W1");
        result.Name.Should().Be("Widget");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByBarcodeAsync_ReturnsDto_WhenFound()
    {
        var product = new Product { Id = 1, Sku = "W1", Name = "Widget", Description = "A widget", Barcode = "123456", Price = 9.99m };
        _repoMock.Setup(r => r.GetByBarcodeAsync("123456")).ReturnsAsync(product);

        var result = await _sut.GetByBarcodeAsync("123456");

        result.Should().NotBeNull();
        result!.Barcode.Should().Be("123456");
        result.Name.Should().Be("Widget");
    }

    [Fact]
    public async Task GetByBarcodeAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByBarcodeAsync("000000")).ReturnsAsync((Product?)null);

        var result = await _sut.GetByBarcodeAsync("000000");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySkuAsync_ReturnsDto_WhenFound()
    {
        var product = new Product { Id = 1, Sku = "W1", Name = "Widget", Description = "A widget", Barcode = "123456", Price = 9.99m };
        _repoMock.Setup(r => r.GetBySkuAsync("W1")).ReturnsAsync(product);

        var result = await _sut.GetBySkuAsync("W1");

        result.Should().NotBeNull();
        result!.Sku.Should().Be("W1");
    }

    [Fact]
    public async Task GetBySkuAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetBySkuAsync("X99")).ReturnsAsync((Product?)null);

        var result = await _sut.GetBySkuAsync("X99");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_AddsEntityAndReturnsDto()
    {
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => p.Id = 1)
            .ReturnsAsync((Product p) => p);

        var request = new CreateProductRequest("W2", "Widget Pro", "Premium widget", "345678", 29.99m);

        var result = await _sut.CreateAsync(request);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Widget Pro");
        result.Price.Should().Be(29.99m);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ThrowsValidationException_WhenInvalid()
    {
        _createValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateProductRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult([
                new FluentValidation.Results.ValidationFailure("Name", "Name is required.")]));

        var request = new CreateProductRequest("W2", "", "Premium widget", "345678", 29.99m);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntityAndReturnsDto()
    {
        var existing = new Product { Id = 1, Sku = "W1", Name = "Widget", Description = "Old", Barcode = "123456", Price = 9.99m };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

        var request = new UpdateProductRequest("W1-V2", "Widget V2", "Updated widget", "123456", 14.99m);

        var result = await _sut.UpdateAsync(1, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Widget V2");
        result.Price.Should().Be(14.99m);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var result = await _sut.UpdateAsync(99, new UpdateProductRequest("X", "X", "X", "X", 0));

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenFound()
    {
        var product = new Product { Id = 1, Sku = "W1", Name = "Widget" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.Remove(product), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var result = await _sut.DeleteAsync(99);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        var products = Enumerable.Range(1, 25)
            .Select(i => new Product { Id = i, Sku = $"P{i}", Name = $"Product {i}", Description = $"Desc {i}", Barcode = $"{i:D6}", Price = i * 10m })
            .ToList();
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);
        _repoMock.Setup(r => r.AsQueryable()).Returns(products.AsAsyncQueryable());

        var result = await _sut.GetPagedAsync(new PagedRequest(Page: 2, PageSize: 10));

        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.HasPrevious.Should().BeTrue();
        result.HasNext.Should().BeTrue();
        result.Items[0].Id.Should().Be(11);
    }
}
