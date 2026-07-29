using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IStoreService
{
    Task<List<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<StoreDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<StoreDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StoreDto> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default);
    Task<StoreDto?> UpdateAsync(int id, UpdateStoreRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
