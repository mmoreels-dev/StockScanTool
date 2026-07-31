using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IScanningDeviceRepository _deviceRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<RefreshToken> _refreshTokenRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwt;
    private readonly IApiKeyHasher _apiKeyHasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IScanningDeviceRepository deviceRepo,
        IRepository<User> userRepo,
        IRepository<RefreshToken> refreshTokenRepo,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwt,
        IApiKeyHasher apiKeyHasher,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _deviceRepo = deviceRepo;
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _unitOfWork = unitOfWork;
        _jwt = jwt;
        _apiKeyHasher = apiKeyHasher;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }

    public async Task<DeviceLoginResponse?> LoginDeviceAsync(DeviceLoginRequest request)
    {
        var hashedKey = _apiKeyHasher.Hash(request.ApiKey.Trim());
        var device = await _deviceRepo.GetByApiKeyAsync(hashedKey);
        if (device is null)
        {
            _logger.LogWarning("Device login failed: no device found for the provided API key.");
            return null;
        }

        if (!device.IsActive)
        {
            _logger.LogWarning("Device login failed: device '{DeviceName}' (Id={DeviceId}) is inactive.", device.DeviceName, device.Id);
            return null;
        }

        if (device.Store is null)
        {
            _logger.LogError("Device login failed: device '{DeviceName}' (Id={DeviceId}) has no associated store.", device.DeviceName, device.Id);
            return null;
        }

        _logger.LogInformation("Device '{DeviceName}' (Id={DeviceId}) logged in to store '{StoreName}'.", device.DeviceName, device.Id, device.Store.Name);

        device.LastPing = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var token = _jwt.GenerateDeviceToken(device.Id, device.StoreId);

        return new DeviceLoginResponse(
            device.Id, device.DeviceName,
            device.StoreId, device.Store.Name,
            token);
    }

    public async Task<AdminLoginResponse?> LoginUserAsync(string username, string password)
    {
        var user = await _userRepo.AsQueryable()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("User login failed for username '{Username}'.", username);
            return null;
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for user '{Username}'.", username);
            return null;
        }

        _logger.LogInformation("User '{Username}' (Id={UserId}) logged in.", user.Username, user.Id);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var token = _jwt.GenerateUserToken(user.Id, user.Username, user.DisplayName, roles, permissions);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);
        await _unitOfWork.SaveChangesAsync();

        return new AdminLoginResponse(token, refreshToken, _jwt.ExpirationInMinutes);
    }

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var stored = await _refreshTokenRepo.AsQueryable()
            .Include(t => t.User)
                .ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (stored is null || !stored.IsActive)
            return null;

        stored.RevokedAt = DateTime.UtcNow;
        _refreshTokenRepo.Update(stored);

        var user = stored.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var newToken = _jwt.GenerateUserToken(user.Id, user.Username, user.DisplayName, roles, permissions);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id);

        await _unitOfWork.SaveChangesAsync();

        return new RefreshTokenResponse(newToken, newRefreshToken, _jwt.ExpirationInMinutes);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string? refreshToken, int? userId = null)
    {
        IQueryable<RefreshToken> query = _refreshTokenRepo.AsQueryable();

        if (refreshToken is not null)
            query = query.Where(t => t.Token == refreshToken);
        else if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value && t.RevokedAt == null);
        else
            return false;

        var tokens = await query.ToListAsync();
        if (tokens.Count == 0)
            return false;

        foreach (var token in tokens)
            token.RevokedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private async Task<string> CreateRefreshTokenAsync(int userId)
    {
        var token = new RefreshToken
        {
            UserId = userId,
            Token = _jwt.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.Value.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepo.AddAsync(token);
        return token.Token;
    }
}
