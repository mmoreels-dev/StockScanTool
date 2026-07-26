using StockScanTool.Application.Results;
using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync();
    Task<PagedResult<ProductDto>> GetPagedAsync(PagedRequest request);
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto?> GetByBarcodeAsync(string barcode);
    Task<ProductDto?> GetBySkuAsync(string sku);
    Task<ProductDto> CreateAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteAsync(int id);
}
