using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Role> _roleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserService(
        IRepository<User> userRepo,
        IRepository<Role> roleRepo,
        IUnitOfWork unitOfWork,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepo.AsQueryable()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.AsQueryable()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.AsQueryable()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        if (await _userRepo.AsQueryable().AnyAsync(u => u.Username == request.Username, cancellationToken))
            throw new ValidationException($"A user with username '{request.Username}' already exists.");

        var roles = await _roleRepo.AsQueryable()
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var user = new User
        {
            Username = request.Username,
            PasswordHash = PasswordHasher.Hash(request.Password),
            DisplayName = request.DisplayName,
            IsActive = true,
            UserRoles = roles.Select(r => new UserRole { Role = r }).ToList()
        };

        await _userRepo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(user);
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var user = await _userRepo.AsQueryable()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return null;

        if (await _userRepo.AsQueryable().AnyAsync(u => u.Username == request.Username && u.Id != id, cancellationToken))
            throw new ValidationException($"A user with username '{request.Username}' already exists.");

        user.Username = request.Username;
        user.DisplayName = request.DisplayName;
        user.IsActive = request.IsActive;

        user.UserRoles.Clear();
        var roles = await _roleRepo.AsQueryable()
            .Where(r => request.RoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);
        foreach (var role in roles)
            user.UserRoles.Add(new UserRole { Role = role });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(user);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user is null) return false;

        _userRepo.Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static UserDto MapToDto(User user) => new(
        user.Id, user.Username, user.DisplayName, user.IsActive,
        user.UserRoles.Select(ur => ur.Role.Name).ToList());
}
