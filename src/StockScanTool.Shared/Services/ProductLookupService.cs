using StockScanTool.Contracts;

namespace StockScanTool.Shared.Services;

public class ProductLookupService : BaseApiService
{
    public ProductLookupService(HttpClient http) : base(http) { }

    public async Task<BarcodeLookupResponse?> LookupBarcodeAsync(string barcode, int storeId)
        => await GetAsync<BarcodeLookupResponse>($"api/v1/sales/lookup/{barcode}?storeId={storeId}");
}
