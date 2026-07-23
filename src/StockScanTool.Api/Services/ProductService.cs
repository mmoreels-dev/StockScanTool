using Microsoft.EntityFrameworkCore;
using StockScanTool.Contracts;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Api.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db) => _db = db;

    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _db.Products
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Description, p.Barcode, p.Price))
            .ToListAsync();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        return p is null ? null : new ProductDto(p.Id, p.Sku, p.Name, p.Description, p.Barcode, p.Price);
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Barcode == barcode);
        return p is null ? null : new ProductDto(p.Id, p.Sku, p.Name, p.Description, p.Barcode, p.Price);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        var product = new Domain.Entities.Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            Barcode = request.Barcode,
            Price = request.Price
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return new ProductDto(product.Id, product.Sku, product.Name, product.Description, product.Barcode, product.Price);
    }

    public async Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return null;

        product.Sku = request.Sku;
        product.Name = request.Name;
        product.Description = request.Description;
        product.Barcode = request.Barcode;
        product.Price = request.Price;
        await _db.SaveChangesAsync();
        return new ProductDto(product.Id, product.Sku, product.Name, product.Description, product.Barcode, product.Price);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return false;
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return true;
    }
}
