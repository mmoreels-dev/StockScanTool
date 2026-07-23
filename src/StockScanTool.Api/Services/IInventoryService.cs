using StockScanTool.Contracts;

namespace StockScanTool.Api.Services;

public interface IInventoryService
{
    Task<List<InventoryDto>> GetAllAsync();
    Task<List<InventoryDto>> GetByStoreAsync(int storeId);
    Task<InventoryDto?> GetByIdAsync(int id);
    Task<InventoryDto> UpsertAsync(UpdateInventoryRequest request);
}
