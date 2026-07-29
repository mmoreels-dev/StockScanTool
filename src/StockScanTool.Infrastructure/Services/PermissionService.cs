using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IRepository<Permission> _permissionRepo;

    public PermissionService(IRepository<Permission> permissionRepo)
    {
        _permissionRepo = permissionRepo;
    }

    public async Task<List<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepo.AsQueryable()
            .OrderBy(p => p.GroupName).ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);
        return permissions.Select(p => new PermissionDto(p.Id, p.Code, p.Name, p.Description, p.GroupName)).ToList();
    }
}
