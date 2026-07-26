using StockScanTool.Domain.Entities;

namespace StockScanTool.Application.Repositories;

public interface IInventoryRepository : IRepository<Inventory>
{
    Task<Inventory?> GetByProductAndStoreAsync(int productId, int storeId);
    Task<List<Inventory>> GetByStoreAsync(int storeId);
}
