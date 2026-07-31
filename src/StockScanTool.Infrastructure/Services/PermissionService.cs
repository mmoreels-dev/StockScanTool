using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IRepository<Permission> _permissionRepo;
    private readonly IRepository<RolePermission> _rolePermissionRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePermissionRequest>? _createValidator;
    private readonly IValidator<UpdatePermissionRequest>? _updateValidator;

    public PermissionService(
        IRepository<Permission> permissionRepo,
        IRepository<RolePermission> rolePermissionRepo,
        IUnitOfWork unitOfWork,
        IValidator<CreatePermissionRequest>? createValidator = null,
        IValidator<UpdatePermissionRequest>? updateValidator = null)
    {
        _permissionRepo = permissionRepo;
        _rolePermissionRepo = rolePermissionRepo;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepo.AsQueryable()
            .OrderBy(p => p.GroupName).ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);
        return permissions.Select(ToDto).ToList();
    }

    public async Task<PermissionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepo.GetByIdAsync(id);
        return permission is null ? null : ToDto(permission);
    }

    public async Task<PermissionDto> CreateAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        if (_createValidator is not null)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);
        }

        var code = request.Code.Trim();
        if (await _permissionRepo.AsQueryable().AnyAsync(p => p.Code == code, cancellationToken))
            throw new ValidationException($"A permission with code '{code}' already exists.");

        var entity = new Permission
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            GroupName = request.GroupName.Trim()
        };

        await _permissionRepo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<PermissionDto?> UpdateAsync(int id, UpdatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        if (_updateValidator is not null)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);
        }

        var entity = await _permissionRepo.GetByIdAsync(id);
        if (entity is null) return null;

        var code = request.Code.Trim();
        if (await _permissionRepo.AsQueryable().AnyAsync(p => p.Code == code && p.Id != id, cancellationToken))
            throw new ValidationException($"A permission with code '{code}' already exists.");

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim() ?? string.Empty;
        entity.GroupName = request.GroupName.Trim();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _permissionRepo.GetByIdAsync(id);
        if (entity is null) return false;

        if (await _rolePermissionRepo.AsQueryable().AnyAsync(rp => rp.PermissionId == id, cancellationToken))
            throw new ValidationException(
                $"Permission '{entity.Code}' is assigned to one or more roles. Remove it from those roles before deleting.");

        _permissionRepo.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PermissionDto ToDto(Permission p) => new(p.Id, p.Code, p.Name, p.Description, p.GroupName);
}
