using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PermissionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PermissionDto> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<PermissionDto?> UpdateAsync(int id, UpdatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
