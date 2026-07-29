using StockScanTool.Application.Results;
using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface ISaleService
{
    Task<Result<SaleTransactionDto>> SubmitSaleAsync(SubmitSaleRequest request, CancellationToken cancellationToken = default);
    Task<List<SaleTransactionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<SaleTransactionDto>> GetByStoreAsync(int storeId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
