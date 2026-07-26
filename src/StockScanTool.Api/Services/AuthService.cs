using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Services;

public class AuthService
{
    private readonly IScanningDeviceRepository _deviceRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwt;

    public AuthService(
        IScanningDeviceRepository deviceRepo,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwt)
    {
        _deviceRepo = deviceRepo;
        _unitOfWork = unitOfWork;
        _jwt = jwt;
    }

    public async Task<DeviceLoginResponse?> LoginAsync(DeviceLoginRequest request)
    {
        var device = await _deviceRepo.GetByApiKeyAsync(request.ApiKey);
        if (device is null || !device.IsActive) return null;

        device.LastPing = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var token = _jwt.GenerateDeviceToken(device.Id, device.StoreId);

        return new DeviceLoginResponse(
            device.Id, device.DeviceName,
            device.StoreId, device.Store.Name,
            token);
    }
}
