using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Infrastructure.Repositories;

public class SaleTransactionRepository : Repository<SaleTransaction>, ISaleTransactionRepository
{
    public SaleTransactionRepository(AppDbContext db) : base(db) { }

    public async Task<SaleTransaction?> GetByIdWithIncludesAsync(int id)
        => await _set
            .Include(s => s.Store)
            .Include(s => s.ScanningDevice)
            .Include(s => s.SaleItems).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id);

    public override async Task<List<SaleTransaction>> GetAllAsync()
        => await _set
            .Include(s => s.Store)
            .Include(s => s.ScanningDevice)
            .Include(s => s.SaleItems).ThenInclude(i => i.Product)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

    public async Task<List<SaleTransaction>> GetByStoreWithDateFilterAsync(int storeId, DateTime? from, DateTime? to)
    {
        var query = _set
            .Include(s => s.Store)
            .Include(s => s.ScanningDevice)
            .Include(s => s.SaleItems).ThenInclude(i => i.Product)
            .Where(s => s.StoreId == storeId)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.SaleDate <= to.Value);

        return await query.OrderByDescending(s => s.SaleDate).ToListAsync();
    }
}
