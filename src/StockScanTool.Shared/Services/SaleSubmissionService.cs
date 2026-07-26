using StockScanTool.Contracts;

namespace StockScanTool.Shared.Services;

public class SaleSubmissionService : BaseApiService
{
    public SaleSubmissionService(HttpClient http) : base(http) { }

    public async Task<SaleTransactionDto?> SubmitSaleAsync(int storeId, int deviceId, List<CartItem> cart)
    {
        var request = new SubmitSaleRequest(
            storeId,
            deviceId,
            cart.Select(c => new SubmitSaleItemRequest(c.ProductId, c.Quantity)).ToList());

        return await PostAsync<SaleTransactionDto, SubmitSaleRequest>("api/v1/sales", request);
    }
}
