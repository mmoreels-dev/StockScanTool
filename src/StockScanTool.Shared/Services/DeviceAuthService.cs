using System.Text.Json;
using Microsoft.JSInterop;
using StockScanTool.Contracts;

namespace StockScanTool.Shared.Services;

public class DeviceAuthService : BaseApiService
{
    private const string TokenKey = "sst_device_token";
    private const string DeviceInfoKey = "sst_device_info";
    private const string ServerUrlKey = "sst_device_server_url";

    private readonly IJSRuntime _js;

    private int _deviceId;
    private int _storeId;
    private string _storeName = string.Empty;
    private string _deviceName = string.Empty;

    public DeviceAuthService(HttpClient http, IJSRuntime js) : base(http)
    {
        _js = js;
    }

    public int DeviceId => _deviceId;
    public int StoreId => _storeId;
    public string StoreName => _storeName;
    public string DeviceName => _deviceName;

    public override void SetBaseUrl(string url)
    {
        base.SetBaseUrl(url);
        _ = PersistStoredAsync(ServerUrlKey, url);
    }

    public async Task InitializeAsync()
    {
        var token = await GetStoredAsync(TokenKey);
        if (string.IsNullOrEmpty(token))
            return;

        var infoJson = await GetStoredAsync(DeviceInfoKey);
        if (string.IsNullOrEmpty(infoJson))
            return;

        try
        {
            var info = JsonSerializer.Deserialize<DeviceInfo>(infoJson);
            if (info is null)
                return;

            var serverUrl = await GetStoredAsync(ServerUrlKey);
            if (!string.IsNullOrEmpty(serverUrl))
                base.SetBaseUrl(serverUrl);

            _token = token;
            _deviceId = info.DeviceId;
            _storeId = info.StoreId;
            _storeName = info.StoreName;
            _deviceName = info.DeviceName;
            AttachToken();
        }
        catch (JsonException)
        {
            // Corrupted storage — treat as logged out.
        }
    }

    public async Task<bool> LoginAsync(string apiKey)
    {
        var result = await PostAsync<DeviceLoginResponse, DeviceLoginRequest>(
            "api/v1/auth/device-login", new DeviceLoginRequest(apiKey));

        if (result is null) return false;

        _token = result.Token;
        _deviceId = result.DeviceId;
        _storeId = result.StoreId;
        _storeName = result.StoreName;
        _deviceName = result.DeviceName;
        AttachToken();
        await PersistAsync();
        return true;
    }

    public async Task Logout()
    {
        _token = string.Empty;
        _deviceId = 0;
        _storeId = 0;
        _storeName = string.Empty;
        _deviceName = string.Empty;
        ClearToken();
        await RemoveStoredAsync(TokenKey);
        await RemoveStoredAsync(DeviceInfoKey);
    }

    private async Task PersistAsync()
    {
        var info = JsonSerializer.Serialize(new DeviceInfo(_deviceId, _storeId, _storeName, _deviceName));
        await PersistStoredAsync(TokenKey, _token);
        await PersistStoredAsync(DeviceInfoKey, info);
    }

    private async Task PersistStoredAsync(string key, string value)
    {
        try
        {
            await _js.InvokeVoidAsync("setStorageItem", key, value);
        }
        catch
        {
            // storage unavailable — session works in-memory only
        }
    }

    private async Task<string?> GetStoredAsync(string key)
    {
        try
        {
            var value = await _js.InvokeAsync<string>("getStorageItem", key);
            return string.IsNullOrEmpty(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    private async Task RemoveStoredAsync(string key)
    {
        try
        {
            await _js.InvokeVoidAsync("removeStorageItem", key);
        }
        catch
        {
            // ignore
        }
    }

    private sealed record DeviceInfo(int DeviceId, int StoreId, string StoreName, string DeviceName);
}
