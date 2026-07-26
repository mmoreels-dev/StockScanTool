using StockScanTool.Domain.Entities;

namespace StockScanTool.Application.Repositories;

public interface IScanningDeviceRepository : IRepository<ScanningDevice>
{
    Task<ScanningDevice?> GetByApiKeyAsync(string apiKey);
}
