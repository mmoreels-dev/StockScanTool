using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Infrastructure.Repositories;

public class StoreRepository : Repository<Store>, IStoreRepository
{
    public StoreRepository(AppDbContext db) : base(db) { }

    public async Task<Store?> GetByNameAsync(string name)
        => await _set.FirstOrDefaultAsync(s => s.Name == name);
}
