using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IAuthService
{
    Task<DeviceLoginResponse?> LoginDeviceAsync(DeviceLoginRequest request);
    Task<AdminLoginResponse?> LoginUserAsync(string username, string password);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeRefreshTokenAsync(string? refreshToken, int? userId = null);
}