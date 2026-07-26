using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext db) : base(db) { }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
        => await _set.FirstOrDefaultAsync(p => p.Barcode == barcode);

    public async Task<Product?> GetBySkuAsync(string sku)
        => await _set.FirstOrDefaultAsync(p => p.Sku == sku);
}
