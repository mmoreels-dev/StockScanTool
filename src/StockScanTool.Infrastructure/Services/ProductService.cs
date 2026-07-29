using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class ProductService : CrudService<Product, ProductDto, CreateProductRequest, UpdateProductRequest>, IProductService
{
    private readonly IProductRepository _productRepo;

    public ProductService(
        IProductRepository repo,
        IUnitOfWork unitOfWork,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator)
        : base(repo, unitOfWork, createValidator, updateValidator)
    {
        _productRepo = repo;
    }

    protected override Expression<Func<Product, bool>> IdPredicate(int id)
        => p => p.Id == id;

    protected override ProductDto ToDto(Product p) => EntityMapper.ToDto(p);

    protected override Product ToEntity(CreateProductRequest r)
        => new() { Sku = r.Sku, Name = r.Name, Description = r.Description, Barcode = r.Barcode, Price = r.Price };

    protected override void UpdateEntity(Product p, UpdateProductRequest r)
    {
        p.Sku = r.Sku;
        p.Name = r.Name;
        p.Description = r.Description;
        p.Barcode = r.Barcode;
        p.Price = r.Price;
    }

    public override async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateUniquenessAsync(request.Sku, request.Barcode, null, cancellationToken);
        return await base.CreateAsync(request, cancellationToken);
    }

    public override async Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateUniquenessAsync(request.Sku, request.Barcode, id, cancellationToken);
        return await base.UpdateAsync(id, request, cancellationToken);
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var product = await _productRepo.GetByBarcodeAsync(barcode);
        return product is null ? null : ToDto(product);
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var product = await _productRepo.GetBySkuAsync(sku);
        return product is null ? null : ToDto(product);
    }

    public async Task<ProductDto?> UpdateImagePathAsync(int id, string? imagePath, CancellationToken cancellationToken = default)
    {
        var product = await _productRepo.GetByIdAsync(id);
        if (product is null) return null;

        product.ImagePath = imagePath;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    private async Task ValidateUniquenessAsync(string sku, string barcode, int? excludeId, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        var existingBySku = await _productRepo.GetBySkuAsync(sku);
        if (existingBySku is not null && existingBySku.Id != excludeId)
            errors.Add($"A product with SKU '{sku}' already exists.");

        var existingByBarcode = await _productRepo.GetByBarcodeAsync(barcode);
        if (existingByBarcode is not null && existingByBarcode.Id != excludeId)
            errors.Add($"A product with barcode '{barcode}' already exists.");

        if (errors.Count > 0)
            throw new ValidationException(errors.Select(e => new ValidationFailure("", e)).ToList());
    }
}
