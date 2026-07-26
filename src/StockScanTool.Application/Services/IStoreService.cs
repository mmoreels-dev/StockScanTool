using StockScanTool.Application.Results;
using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IStoreService
{
    Task<List<StoreDto>> GetAllAsync();
    Task<PagedResult<StoreDto>> GetPagedAsync(PagedRequest request);
    Task<StoreDto?> GetByIdAsync(int id);
    Task<StoreDto> CreateAsync(CreateStoreRequest request);
    Task<StoreDto?> UpdateAsync(int id, UpdateStoreRequest request);
    Task<bool> DeleteAsync(int id);
}
