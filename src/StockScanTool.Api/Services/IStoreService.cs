using StockScanTool.Contracts;

namespace StockScanTool.Api.Services;

public interface IStoreService
{
    Task<List<StoreDto>> GetAllAsync();
    Task<StoreDto?> GetByIdAsync(int id);
    Task<StoreDto> CreateAsync(CreateStoreRequest request);
    Task<StoreDto?> UpdateAsync(int id, UpdateStoreRequest request);
    Task<bool> DeleteAsync(int id);
}
