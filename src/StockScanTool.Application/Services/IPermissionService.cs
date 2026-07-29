using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
