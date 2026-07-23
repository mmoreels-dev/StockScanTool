using System.Net.Http.Json;
using StockScanTool.Contracts;

namespace StockScanTool.ScannerPwa.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private string _token = string.Empty;

    public ApiService(HttpClient http) => _http = http;

    public int DeviceId { get; private set; }
    public int StoreId { get; private set; }
    public string StoreName { get; private set; } = "";
    public string DeviceName { get; private set; } = "";
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public void SetBaseUrl(string url)
    {
        _http.BaseAddress = new Uri(url.TrimEnd('/') + "/");
    }

    public async Task<bool> LoginAsync(string apiKey)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/devices/login", new DeviceLoginRequest(apiKey));
            if (!resp.IsSuccessStatusCode) return false;

            var result = await resp.Content.ReadFromJsonAsync<DeviceLoginResponse>();
            if (result is null) return false;

            _token = result.Token;
            DeviceId = result.DeviceId;
            StoreId = result.StoreId;
            StoreName = result.StoreName;
            DeviceName = result.DeviceName;
            return true;
        }
        catch { return false; }
    }

    public async Task<BarcodeLookupResponse?> LookupBarcodeAsync(string barcode)
    {
        try
        {
            return await _http.GetFromJsonAsync<BarcodeLookupResponse>(
                $"api/sales/lookup/{barcode}?storeId={StoreId}");
        }
        catch { return null; }
    }

    public async Task<SaleTransactionResponse?> SubmitSaleAsync(List<CartItem> cart)
    {
        try
        {
            var request = new SubmitSaleRequest(StoreId, DeviceId,
                cart.Select(c => new SubmitSaleItemRequest(c.ProductId, c.Quantity)).ToList());

            var resp = await _http.PostAsJsonAsync("api/sales", request);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<SaleTransactionResponse>();
        }
        catch { return null; }
    }

    public void Logout()
    {
        _token = string.Empty;
        DeviceId = 0;
        StoreId = 0;
        StoreName = "";
        DeviceName = "";
    }
}
