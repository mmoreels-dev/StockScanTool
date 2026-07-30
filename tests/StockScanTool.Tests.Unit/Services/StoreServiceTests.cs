using Xunit;
using Moq;
using FluentAssertions;
using MockQueryable;
using StockScanTool.Infrastructure.Services;
using StockScanTool.Application.Repositories;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;
using FluentValidation;

namespace StockScanTool.Tests.Unit.Services;

public class StoreServiceTests
{
    private readonly Mock<IStoreRepository> _repoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IValidator<CreateStoreRequest>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateStoreRequest>> _updateValidatorMock;
    private readonly StoreService _sut;

    public StoreServiceTests()
    {
        _repoMock = new Mock<IStoreRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _createValidatorMock = new Mock<IValidator<CreateStoreRequest>>();
        _updateValidatorMock = new Mock<IValidator<UpdateStoreRequest>>();
        _createValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<CreateStoreRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _updateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateStoreRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _sut = new StoreService(_repoMock.Object, _unitOfWorkMock.Object, _createValidatorMock.Object, _updateValidatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var stores = new List<Store>
        {
            new() { Id = 1, Name = "Store A", Address = "123 Main St", IsActive = true },
            new() { Id = 2, Name = "Store B", Address = "456 Oak Ave", IsActive = false }
        };
        _repoMock.Setup(r => r.AsQueryable()).Returns(stores.BuildMock());

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Store A");
        result[1].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        var store = new Store { Id = 1, Name = "Store A", Address = "123 Main St", IsActive = true };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Store A");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Store?)null);

        var result = await _sut.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_AddsEntityAndReturnsDto()
    {
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Store>()))
            .Callback<Store>(s => s.Id = 1)
            .ReturnsAsync((Store s) => s);

        var request = new CreateStoreRequest("New Store", "789 Pine Rd");

        var result = await _sut.CreateAsync(request);

        result.Id.Should().Be(1);
        result.Name.Should().Be("New Store");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenFound()
    {
        var store = new Store { Id = 1, Name = "Store A" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(store);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
        _repoMock.Verify(r => r.Remove(store), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Store?)null);

        var result = await _sut.DeleteAsync(99);

        result.Should().BeFalse();
    }
}
