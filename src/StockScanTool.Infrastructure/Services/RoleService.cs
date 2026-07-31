using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class RoleService : CrudService<Role, RoleDto, CreateRoleRequest, UpdateRoleRequest>, IRoleService
{
    private readonly IRepository<Permission> _permissionRepo;

    public RoleService(
        IRepository<Role> roleRepo,
        IRepository<Permission> permissionRepo,
        IUnitOfWork unitOfWork,
        IValidator<CreateRoleRequest> createValidator,
        IValidator<UpdateRoleRequest> updateValidator)
        : base(roleRepo, unitOfWork, createValidator, updateValidator)
    {
        _permissionRepo = permissionRepo;
    }

    protected override IQueryable<Role> ApplyIncludes(IQueryable<Role> query)
        => query.Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission);

    protected override RoleDto ToDto(Role role) => new(
        role.Id, role.Name, role.Description, role.IsActive,
        role.RolePermissions.Select(rp => rp.Permission.Code).ToList());

    protected override Role ToEntity(CreateRoleRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
        IsActive = true,
        RolePermissions = request.PermissionIds.Select(id => new RolePermission { PermissionId = id }).ToList()
    };

    protected override void UpdateEntity(Role entity, UpdateRoleRequest request)
    {
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        entity.RolePermissions.Clear();
        foreach (var id in request.PermissionIds)
            entity.RolePermissions.Add(new RolePermission { PermissionId = id });
    }

    public override async Task<RoleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await ApplyIncludes(_repo.AsQueryable())
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return role is null ? null : ToDto(role);
    }

    public override async Task<PagedResult<RoleDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = ApplyIncludes(_repo.AsQueryable());
        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.Descending
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name),
            _ => query.OrderBy(r => r.Id)
        };

        var paged = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoleDto>(paged.Select(ToDto).ToList(), totalCount, request.Page, request.PageSize);
    }

    public override async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (_createValidator is not null)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);
        }

        if (await _repo.AsQueryable().AnyAsync(r => r.Name == request.Name, cancellationToken))
            throw new ValidationException($"A role with name '{request.Name}' already exists.");

        var entity = ToEntity(request);
        var permissions = await _permissionRepo.AsQueryable()
            .Where(p => request.PermissionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        entity.RolePermissions = permissions.Select(p => new RolePermission { Permission = p }).ToList();

        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public override async Task<RoleDto?> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (_updateValidator is not null)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);
        }

        var role = await ApplyIncludes(_repo.AsQueryable())
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null) return null;

        if (await _repo.AsQueryable().AnyAsync(r => r.Name == request.Name && r.Id != id, cancellationToken))
            throw new ValidationException($"A role with name '{request.Name}' already exists.");

        UpdateEntity(role, request);
        var permissions = await _permissionRepo.AsQueryable()
            .Where(p => request.PermissionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        role.RolePermissions.Clear();
        foreach (var permission in permissions)
            role.RolePermissions.Add(new RolePermission { Permission = permission });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(role);
    }
}
