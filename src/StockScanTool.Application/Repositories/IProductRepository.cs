using StockScanTool.Domain.Entities;

namespace StockScanTool.Application.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<Product?> GetBySkuAsync(string sku);
}
