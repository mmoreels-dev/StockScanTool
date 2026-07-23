using Microsoft.EntityFrameworkCore;
using StockScanTool.Contracts;
using StockScanTool.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text;

namespace StockScanTool.Api.Services;

public class DeviceService : IDeviceService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;

    public DeviceService(AppDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<List<DeviceDto>> GetAllAsync()
    {
        return await _db.ScanningDevices
            .Include(d => d.Store)
            .OrderBy(d => d.DeviceName)
            .Select(d => new DeviceDto(
                d.Id, d.DeviceName, d.StoreId, d.Store.Name,
                d.ApiKey, d.IsActive, d.LastPing))
            .ToListAsync();
    }

    public async Task<DeviceDto?> GetByIdAsync(int id)
    {
        var d = await _db.ScanningDevices.Include(x => x.Store).FirstOrDefaultAsync(x => x.Id == id);
        return d is null ? null : new DeviceDto(
            d.Id, d.DeviceName, d.StoreId, d.Store.Name,
            d.ApiKey, d.IsActive, d.LastPing);
    }

    public async Task<DeviceDto> CreateAsync(CreateDeviceRequest request)
    {
        var device = new Domain.Entities.ScanningDevice
        {
            DeviceName = request.DeviceName,
            StoreId = request.StoreId,
            ApiKey = GenerateApiKey(),
            IsActive = true
        };
        _db.ScanningDevices.Add(device);
        await _db.SaveChangesAsync();

        var storeName = await _db.Stores.Where(s => s.Id == request.StoreId).Select(s => s.Name).FirstAsync();
        return new DeviceDto(device.Id, device.DeviceName, device.StoreId, storeName, device.ApiKey, device.IsActive, device.LastPing);
    }

    public async Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceRequest request)
    {
        var device = await _db.ScanningDevices.FindAsync(id);
        if (device is null) return null;

        device.DeviceName = request.DeviceName;
        device.StoreId = request.StoreId;
        device.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var device = await _db.ScanningDevices.FindAsync(id);
        if (device is null) return false;
        _db.ScanningDevices.Remove(device);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DeviceLoginResponse?> LoginAsync(DeviceLoginRequest request)
    {
        var device = await _db.ScanningDevices
            .Include(d => d.Store)
            .FirstOrDefaultAsync(d => d.ApiKey == request.ApiKey && d.IsActive);

        if (device is null) return null;

        device.LastPing = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateDeviceToken(device.Id, device.StoreId);

        return new DeviceLoginResponse(
            device.Id, device.DeviceName,
            device.StoreId, device.Store.Name,
            token);
    }

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
