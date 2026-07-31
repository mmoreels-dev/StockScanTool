using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class UserService : CrudService<User, UserDto, CreateUserRequest, UpdateUserRequest>, IUserService
{
    private readonly IRepository<Role> _roleRepo;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IRepository<User> userRepo,
        IRepository<Role> roleRepo,
        IUnitOfWork unitOfWork,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IPasswordHasher passwordHasher)
        : base(userRepo, unitOfWork, createValidator, updateValidator)
    {
        _roleRepo = roleRepo;
        _passwordHasher = passwordHasher;
    }

    protected override IQueryable<User> ApplyIncludes(IQueryable<User> query)
        => query.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    protected override UserDto ToDto(User user) => new(
        user.Id, user.Username, user.DisplayName, user.IsActive,
        user.UserRoles.Select(ur => ur.Role.Name).ToList());

    protected override User ToEntity(CreateUserRequest request) => new()
    {
        Username = request.Username,
        PasswordHash = _passwordHasher.Hash(request.Password),
        DisplayName = request.DisplayName,
        IsActive = true,
        UserRoles = request.RoleIds.Select(id => new UserRole { RoleId = id }).ToList()
    };

    protected override void UpdateEntity(User entity, UpdateUserRequest request)
    {
        entity.Username = request.Username;
        entity.DisplayName = request.DisplayName;
        entity.IsActive = request.IsActive;
        entity.UserRoles.Clear();
        foreach (var id in request.RoleIds)
            entity.UserRoles.Add(new UserRole { RoleId = id });
    }

    public override async Task<PagedResult<UserDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = ApplyIncludes(_repo.AsQueryable());
        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "username" => request.Descending
                ? query.OrderByDescending(u => u.Username)
                : query.OrderBy(u => u.Username),
            "displayname" => request.Descending
                ? query.OrderByDescending(u => u.DisplayName)
                : query.OrderBy(u => u.DisplayName),
            _ => query.OrderBy(u => u.Id)
        };

        var paged = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(paged.Select(ToDto).ToList(), totalCount, request.Page, request.PageSize);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await ApplyIncludes(_repo.AsQueryable())
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public override async Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await ApplyIncludes(_repo.AsQueryable())
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user is null ? null : ToDto(user);
    }

    public override async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (_createValidator is not null)
        {
            var validation = await _createValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);
        }

        if (await _repo.AsQueryable().AnyAsync(u => u.Username == request.Username, cancellationToken))
            throw new ValidationException($"A user with username '{request.Username}' already exists.");

        var entity = ToEntity(request);
        var roles = await _roleRepo.AsQueryable()
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);
        entity.UserRoles = roles.Select(r => new UserRole { Role = r }).ToList();

        await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public override async Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (_updateValidator is not null)
        {
            var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new ValidationException(validation.Errors);
        }

        var user = await ApplyIncludes(_repo.AsQueryable())
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return null;

        if (await _repo.AsQueryable().AnyAsync(u => u.Username == request.Username && u.Id != id, cancellationToken))
            throw new ValidationException($"A user with username '{request.Username}' already exists.");

        UpdateEntity(user, request);
        var roles = await _roleRepo.AsQueryable()
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);
        user.UserRoles.Clear();
        foreach (var role in roles)
            user.UserRoles.Add(new UserRole { Role = role });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }
}
