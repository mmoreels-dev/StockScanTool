using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IScanningDeviceRepository _deviceRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IScanningDeviceRepository deviceRepo,
        IRepository<User> userRepo,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwt,
        ILogger<AuthService> logger)
    {
        _deviceRepo = deviceRepo;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<DeviceLoginResponse?> LoginDeviceAsync(DeviceLoginRequest request)
    {
        var hashedKey = ApiKeyHasher.Hash(request.ApiKey);
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

        if (!PasswordHasher.Verify(password, user.PasswordHash))
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

        return new AdminLoginResponse(token);
    }
}
