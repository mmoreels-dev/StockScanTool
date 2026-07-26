using StockScanTool.Contracts;
using StockScanTool.Shared.Services;

namespace StockScanTool.Scanner.Services;

public class ApiService
{
    private readonly DeviceAuthService _auth;
    private readonly ProductLookupService _lookup;
    private readonly SaleSubmissionService _sale;
    private readonly HttpClient _http;

    public ApiService(HttpClient http)
    {
        _http = http;
        _auth = new DeviceAuthService(http);
        _lookup = new ProductLookupService(http);
        _sale = new SaleSubmissionService(http);
    }

    public string BaseUrl
    {
        get => _http.BaseAddress?.ToString().TrimEnd('/') ?? "";
        set => _auth.SetBaseUrl(value);
    }

    public bool IsAuthenticated => _auth.IsAuthenticated;
    public int DeviceId => _auth.DeviceId;
    public int StoreId => _auth.StoreId;
    public string StoreName => _auth.StoreName;
    public string DeviceName => _auth.DeviceName;

    public Task<bool> LoginAsync(string apiKey) => _auth.LoginAsync(apiKey);

    public Task<BarcodeLookupResponse?> LookupBarcodeAsync(string barcode)
        => _lookup.LookupBarcodeAsync(barcode, _auth.StoreId);

    public Task<SaleTransactionDto?> SubmitSaleAsync(List<CartItem> cart)
        => _sale.SubmitSaleAsync(_auth.StoreId, _auth.DeviceId, cart);

    public void Logout() => _auth.Logout();
}
