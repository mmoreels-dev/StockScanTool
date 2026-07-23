using Microsoft.EntityFrameworkCore;
using StockScanTool.Contracts;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Api.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db) => _db = db;

    public async Task<List<InventoryDto>> GetAllAsync()
    {
        return await _db.Inventories
            .Include(i => i.Product)
            .Include(i => i.Store)
            .OrderBy(i => i.Store.Name)
            .ThenBy(i => i.Product.Name)
            .Select(i => new InventoryDto(
                i.Id, i.ProductId, i.Product.Name, i.Product.Barcode,
                i.StoreId, i.Store.Name, i.QuantityOnHand))
            .ToListAsync();
    }

    public async Task<List<InventoryDto>> GetByStoreAsync(int storeId)
    {
        return await _db.Inventories
            .Include(i => i.Product)
            .Include(i => i.Store)
            .Where(i => i.StoreId == storeId)
            .OrderBy(i => i.Product.Name)
            .Select(i => new InventoryDto(
                i.Id, i.ProductId, i.Product.Name, i.Product.Barcode,
                i.StoreId, i.Store.Name, i.QuantityOnHand))
            .ToListAsync();
    }

    public async Task<InventoryDto?> GetByIdAsync(int id)
    {
        var i = await _db.Inventories
            .Include(x => x.Product)
            .Include(x => x.Store)
            .FirstOrDefaultAsync(x => x.Id == id);

        return i is null ? null : new InventoryDto(
            i.Id, i.ProductId, i.Product.Name, i.Product.Barcode,
            i.StoreId, i.Store.Name, i.QuantityOnHand);
    }

    public async Task<InventoryDto> UpsertAsync(UpdateInventoryRequest request)
    {
        var existing = await _db.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.StoreId == request.StoreId);

        if (existing is not null)
        {
            existing.QuantityOnHand = request.QuantityOnHand;
        }
        else
        {
            existing = new Domain.Entities.Inventory
            {
                ProductId = request.ProductId,
                StoreId = request.StoreId,
                QuantityOnHand = request.QuantityOnHand
            };
            _db.Inventories.Add(existing);
        }

        await _db.SaveChangesAsync();
        return (await GetByIdAsync(existing.Id))!;
    }
}
