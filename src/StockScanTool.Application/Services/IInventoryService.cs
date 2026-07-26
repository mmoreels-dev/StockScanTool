using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IInventoryService
{
    Task<List<InventoryDto>> GetAllAsync();
    Task<List<InventoryDto>> GetByStoreAsync(int storeId);
    Task<InventoryDto?> GetByIdAsync(int id);
    Task<InventoryDto> UpsertAsync(UpdateInventoryRequest request);
    Task<bool> DecrementStockAsync(int productId, int storeId, int quantity);
}
