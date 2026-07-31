using Microsoft.JSInterop;
using StockScanTool.Contracts;
using StockScanTool.Shared.Services;

namespace StockScanTool.ScannerPwa.Services;

public class ApiService
{
    private readonly DeviceAuthService _auth;
    private readonly ProductLookupService _lookup;
    private readonly SaleSubmissionService _sale;

    public ApiService(HttpClient http, IJSRuntime js)
    {
        _auth = new DeviceAuthService(http, js);
        _lookup = new ProductLookupService(http);
        _sale = new SaleSubmissionService(http);
        _lookup.SessionExpired = () => { _ = _auth.Logout(); };
        _sale.SessionExpired = () => { _ = _auth.Logout(); };
    }

    public int DeviceId => _auth.DeviceId;
    public int StoreId => _auth.StoreId;
    public string StoreName => _auth.StoreName;
    public string DeviceName => _auth.DeviceName;
    public bool IsAuthenticated => _auth.IsAuthenticated;

    public void SetBaseUrl(string url)
    {
        _auth.SetBaseUrl(url);
        _lookup.SetBaseUrl(url);
        _sale.SetBaseUrl(url);
    }

    public async Task InitializeAsync()
    {
        await _auth.InitializeAsync();
        var baseUrl = _auth.GetBaseUrl();
        if (!string.IsNullOrEmpty(baseUrl))
            SetBaseUrl(baseUrl);
    }

    public Task<bool> LoginAsync(string apiKey) => _auth.LoginAsync(apiKey);

    public Task<BarcodeLookupResponse?> LookupBarcodeAsync(string barcode)
        => _lookup.LookupBarcodeAsync(barcode, _auth.StoreId);

    public Task<SaleTransactionDto?> SubmitSaleAsync(List<CartItem> cart)
        => _sale.SubmitSaleAsync(_auth.StoreId, _auth.DeviceId, cart);

    public Task Logout() => _auth.Logout();
}
