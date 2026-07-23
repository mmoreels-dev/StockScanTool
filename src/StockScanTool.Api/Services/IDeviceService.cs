using StockScanTool.Contracts;

namespace StockScanTool.Api.Services;

public interface IDeviceService
{
    Task<List<DeviceDto>> GetAllAsync();
    Task<DeviceDto?> GetByIdAsync(int id);
    Task<DeviceDto> CreateAsync(CreateDeviceRequest request);
    Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceRequest request);
    Task<bool> DeleteAsync(int id);
    Task<DeviceLoginResponse?> LoginAsync(DeviceLoginRequest request);
}
