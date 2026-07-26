using StockScanTool.Application.Results;
using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface ISaleService
{
    Task<Result<SaleTransactionDto>> SubmitSaleAsync(SubmitSaleRequest request);
    Task<List<SaleTransactionDto>> GetAllAsync();
    Task<List<SaleTransactionDto>> GetByStoreAsync(int storeId, DateTime? from, DateTime? to);
}
