using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ProductDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateImagePathAsync(int id, string? imagePath, CancellationToken cancellationToken = default);
}
