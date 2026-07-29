namespace StockScanTool.Application.Services;

public interface IJwtTokenService
{
    string GenerateDeviceToken(int deviceId, int storeId);
    string GenerateUserToken(int userId, string username, string displayName, IList<string> roles, IList<string> permissions);
}
