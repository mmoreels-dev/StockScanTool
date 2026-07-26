using StockScanTool.Contracts;

namespace StockScanTool.Shared.Services;

public class DeviceAuthService : BaseApiService
{
    public int DeviceId { get; private set; }
    public int StoreId { get; private set; }
    public string StoreName { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;

    public DeviceAuthService(HttpClient http) : base(http) { }

    public async Task<bool> LoginAsync(string apiKey)
    {
        var result = await PostAsync<DeviceLoginResponse, DeviceLoginRequest>(
            "api/v1/auth/device-login", new DeviceLoginRequest(apiKey));

        if (result is null) return false;

        _token = result.Token;
        DeviceId = result.DeviceId;
        StoreId = result.StoreId;
        StoreName = result.StoreName;
        DeviceName = result.DeviceName;
        AttachToken();
        return true;
    }

    public void Logout()
    {
        _token = string.Empty;
        DeviceId = 0;
        StoreId = 0;
        StoreName = string.Empty;
        DeviceName = string.Empty;
        ClearToken();
    }
}
