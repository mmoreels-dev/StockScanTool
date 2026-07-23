using System.Net.Http.Json;
using StockScanTool.Contracts;

namespace StockScanTool.Scanner.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private string _token = string.Empty;
    private int _deviceId;
    private int _storeId;
    private string _storeName = string.Empty;
    private string _deviceName = string.Empty;

    public ApiService(HttpClient http)
    {
        _http = http;
    }

    public string BaseUrl
    {
        get => _http.BaseAddress?.ToString().TrimEnd('/') ?? "";
        set => _http.BaseAddress = new Uri(value.TrimEnd('/') + "/");
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
    public int DeviceId => _deviceId;
    public int StoreId => _storeId;
    public string StoreName => _storeName;
    public string DeviceName => _deviceName;

    public async Task<bool> LoginAsync(string apiKey)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/devices/login", new DeviceLoginRequest(apiKey));
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<DeviceLoginResponse>();
            if (result is null) return false;

            _token = result.Token;
            _deviceId = result.DeviceId;
            _storeId = result.StoreId;
            _storeName = result.StoreName;
            _deviceName = result.DeviceName;

            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<BarcodeLookupResponse?> LookupBarcodeAsync(string barcode)
    {
        try
        {
            return await _http.GetFromJsonAsync<BarcodeLookupResponse>(
                $"api/sales/lookup/{barcode}?storeId={_storeId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<SaleTransactionDto?> SubmitSaleAsync(List<CartItem> cart)
    {
        try
        {
            var request = new SubmitSaleRequest(
                _storeId,
                _deviceId,
                cart.Select(c => new SubmitSaleItemRequest(c.ProductId, c.Quantity)).ToList()
            );

            var response = await _http.PostAsJsonAsync("api/sales", request);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<SaleTransactionDto>();
        }
        catch
        {
            return null;
        }
    }

    public void Logout()
    {
        _token = string.Empty;
        _deviceId = 0;
        _storeId = 0;
        _storeName = string.Empty;
        _deviceName = string.Empty;
        _http.DefaultRequestHeaders.Authorization = null;
    }
}
