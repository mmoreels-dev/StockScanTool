using StockScanTool.Contracts;

namespace StockScanTool.Api.Services;

public interface ISaleService
{
    Task<SaleTransactionDto?> SubmitSaleAsync(SubmitSaleRequest request);
    Task<List<SaleTransactionDto>> GetSalesByStoreAsync(int storeId, DateTime? from, DateTime? to);
    Task<List<SaleTransactionDto>> GetAllAsync();
    Task<DashboardSummaryDto> GetDashboardAsync();
    Task<BarcodeLookupResponse?> LookupBarcodeAsync(string barcode, int storeId);
}
