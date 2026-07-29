using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly IRepository<Role> _roleRepo;
    private readonly IRepository<Permission> _permissionRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateRoleRequest> _createValidator;
    private readonly IValidator<UpdateRoleRequest> _updateValidator;

    public RoleService(
        IRepository<Role> roleRepo,
        IRepository<Permission> permissionRepo,
        IUnitOfWork unitOfWork,
        IValidator<CreateRoleRequest> createValidator,
        IValidator<UpdateRoleRequest> updateValidator)
    {
        _roleRepo = roleRepo;
        _permissionRepo = permissionRepo;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepo.AsQueryable()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
        return roles.Select(MapToDto).ToList();
    }

    public async Task<RoleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepo.AsQueryable()
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return role is null ? null : MapToDto(role);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        if (await _roleRepo.AsQueryable().AnyAsync(r => r.Name == request.Name, cancellationToken))
            throw new ValidationException($"A role with name '{request.Name}' already exists.");

        var permissions = await _permissionRepo.AsQueryable()
            .Where(p => request.PermissionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            RolePermissions = permissions.Select(p => new RolePermission { Permission = p }).ToList()
        };

        await _roleRepo.AddAsync(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(role);
    }

    public async Task<RoleDto?> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var role = await _roleRepo.AsQueryable()
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null) return null;

        if (await _roleRepo.AsQueryable().AnyAsync(r => r.Name == request.Name && r.Id != id, cancellationToken))
            throw new ValidationException($"A role with name '{request.Name}' already exists.");

        role.Name = request.Name;
        role.Description = request.Description;
        role.IsActive = request.IsActive;

        role.RolePermissions.Clear();
        var permissions = await _permissionRepo.AsQueryable()
            .Where(p => request.PermissionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        foreach (var permission in permissions)
            role.RolePermissions.Add(new RolePermission { Permission = permission });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(role);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepo.GetByIdAsync(id);
        if (role is null) return false;

        _roleRepo.Remove(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static RoleDto MapToDto(Role role) => new(
        role.Id, role.Name, role.Description, role.IsActive,
        role.RolePermissions.Select(rp => rp.Permission.Code).ToList());
}
