using StockScanTool.Domain.Entities;

namespace StockScanTool.Application.Repositories;

public interface ISaleTransactionRepository : IRepository<SaleTransaction>
{
    Task<SaleTransaction?> GetByIdWithIncludesAsync(int id);
    Task<List<SaleTransaction>> GetByStoreWithDateFilterAsync(int storeId, DateTime? from, DateTime? to);
}
