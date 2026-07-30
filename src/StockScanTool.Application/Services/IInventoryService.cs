using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IInventoryService
{
    Task<List<InventoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<InventoryDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<List<InventoryDto>> GetByStoreAsync(int storeId, CancellationToken cancellationToken = default);
    Task<InventoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InventoryDto> UpsertAsync(UpdateInventoryRequest request, CancellationToken cancellationToken = default);
    Task<bool> DecrementStockAsync(int productId, int storeId, int quantity, CancellationToken cancellationToken = default);
}
