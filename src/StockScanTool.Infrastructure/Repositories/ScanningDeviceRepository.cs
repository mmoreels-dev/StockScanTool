using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Infrastructure.Repositories;

public class ScanningDeviceRepository : Repository<ScanningDevice>, IScanningDeviceRepository
{
    public ScanningDeviceRepository(AppDbContext db) : base(db) { }

    public override async Task<List<ScanningDevice>> GetAllAsync()
        => await _set.Include(d => d.Store).OrderBy(d => d.DeviceName).ToListAsync();

    public async Task<ScanningDevice?> GetByApiKeyAsync(string apiKey)
        => await _set.Include(d => d.Store).FirstOrDefaultAsync(d => d.ApiKey == apiKey);
}
