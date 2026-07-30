using StockScanTool.Contracts;

namespace StockScanTool.Application.Services;

public interface IRoleService
{
    Task<List<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<RoleDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<RoleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<RoleDto?> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
