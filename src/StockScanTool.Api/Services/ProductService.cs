using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Api.Services;

public class ProductService : CrudService<Product, ProductDto, CreateProductRequest, UpdateProductRequest>, IProductService
{
    private readonly IProductRepository _productRepo;

    public ProductService(IProductRepository repo, IUnitOfWork unitOfWork)
        : base(repo, unitOfWork)
    {
        _productRepo = repo;
    }

    protected override System.Linq.Expressions.Expression<Func<Product, bool>> IdPredicate(int id)
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

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode)
    {
        var product = await _productRepo.GetByBarcodeAsync(barcode);
        return product is null ? null : ToDto(product);
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku)
    {
        var product = await _productRepo.GetBySkuAsync(sku);
        return product is null ? null : ToDto(product);
    }
}
