using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Infrastructure.Repositories;

public class InventoryRepository : Repository<Inventory>, IInventoryRepository
{
    public InventoryRepository(AppDbContext db) : base(db) { }

    public override async Task<List<Inventory>> GetAllAsync()
        => await _set.Include(i => i.Product).Include(i => i.Store)
            .OrderBy(i => i.Store.Name).ThenBy(i => i.Product.Name)
            .ToListAsync();

    public async Task<Inventory?> GetByProductAndStoreAsync(int productId, int storeId)
        => await _set.Include(i => i.Product).Include(i => i.Store)
            .FirstOrDefaultAsync(i => i.ProductId == productId && i.StoreId == storeId);

    public async Task<List<Inventory>> GetByStoreAsync(int storeId)
        => await _set.Include(i => i.Product).Include(i => i.Store)
            .Where(i => i.StoreId == storeId)
            .OrderBy(i => i.Product.Name)
            .ToListAsync();
}
