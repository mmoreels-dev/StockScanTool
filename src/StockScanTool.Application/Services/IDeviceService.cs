using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IDeviceService
{
    Task<List<DeviceDto>> GetAllAsync();
    Task<PagedResult<DeviceDto>> GetPagedAsync(PagedRequest request);
    Task<DeviceDto?> GetByIdAsync(int id);
    Task<DeviceDto> CreateAsync(CreateDeviceRequest request);
    Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceRequest request);
    Task<bool> DeleteAsync(int id);
}
