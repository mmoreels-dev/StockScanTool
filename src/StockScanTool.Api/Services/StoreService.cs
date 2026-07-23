using Microsoft.EntityFrameworkCore;
using StockScanTool.Contracts;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Api.Services;

public class StoreService : IStoreService
{
    private readonly AppDbContext _db;

    public StoreService(AppDbContext db) => _db = db;

    public async Task<List<StoreDto>> GetAllAsync()
    {
        return await _db.Stores
            .OrderBy(s => s.Name)
            .Select(s => new StoreDto(s.Id, s.Name, s.Address, s.IsActive))
            .ToListAsync();
    }

    public async Task<StoreDto?> GetByIdAsync(int id)
    {
        var s = await _db.Stores.FindAsync(id);
        return s is null ? null : new StoreDto(s.Id, s.Name, s.Address, s.IsActive);
    }

    public async Task<StoreDto> CreateAsync(CreateStoreRequest request)
    {
        var store = new Domain.Entities.Store
        {
            Name = request.Name,
            Address = request.Address,
            IsActive = request.IsActive
        };
        _db.Stores.Add(store);
        await _db.SaveChangesAsync();
        return new StoreDto(store.Id, store.Name, store.Address, store.IsActive);
    }

    public async Task<StoreDto?> UpdateAsync(int id, UpdateStoreRequest request)
    {
        var store = await _db.Stores.FindAsync(id);
        if (store is null) return null;

        store.Name = request.Name;
        store.Address = request.Address;
        store.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return new StoreDto(store.Id, store.Name, store.Address, store.IsActive);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var store = await _db.Stores.FindAsync(id);
        if (store is null) return false;
        _db.Stores.Remove(store);
        await _db.SaveChangesAsync();
        return true;
    }
}
