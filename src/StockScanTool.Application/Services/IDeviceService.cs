using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IDeviceService
{
    Task<List<DeviceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<DeviceDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<DeviceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DeviceDto> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken = default);
    Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceRequest request, CancellationToken cancellationToken = default);
    Task<DeviceDto?> RegenerateApiKeyAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
