using StockScanTool.Domain.Entities;

namespace StockScanTool.Application.Repositories;

public interface IStoreRepository : IRepository<Store>
{
    Task<Store?> GetByNameAsync(string name);
}
