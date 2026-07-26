using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IJwtTokenService
{
    string GenerateDeviceToken(int deviceId, int storeId);
    string GenerateAdminToken();
}
