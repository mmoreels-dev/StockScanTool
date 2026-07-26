using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;
using System.Security.Cryptography;

namespace StockScanTool.Api.Services;

public class DeviceService : CrudService<ScanningDevice, DeviceDto, CreateDeviceRequest, UpdateDeviceRequest>, IDeviceService
{
    private readonly IScanningDeviceRepository _deviceRepo;
    private readonly IStoreRepository _storeRepo;

    public DeviceService(
        IScanningDeviceRepository deviceRepo,
        IStoreRepository storeRepo,
        IUnitOfWork unitOfWork)
        : base(deviceRepo, unitOfWork)
    {
        _deviceRepo = deviceRepo;
        _storeRepo = storeRepo;
    }

    protected override System.Linq.Expressions.Expression<Func<ScanningDevice, bool>> IdPredicate(int id)
        => d => d.Id == id;

    protected override DeviceDto ToDto(ScanningDevice d)
    {
        var storeName = _storeRepo.GetByIdAsync(d.StoreId).GetAwaiter().GetResult()?.Name ?? string.Empty;
        return EntityMapper.ToDto(d, storeName);
    }

    protected override ScanningDevice ToEntity(CreateDeviceRequest r)
        => new()
        {
            DeviceName = r.DeviceName,
            StoreId = r.StoreId,
            ApiKey = GenerateApiKey(),
            IsActive = true
        };

    protected override void UpdateEntity(ScanningDevice d, UpdateDeviceRequest r)
    {
        d.DeviceName = r.DeviceName;
        d.StoreId = r.StoreId;
        d.IsActive = r.IsActive;
    }

    public override async Task<DeviceDto> CreateAsync(CreateDeviceRequest request)
    {
        var entity = ToEntity(request);
        await _deviceRepo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var storeName = (await _storeRepo.GetByIdAsync(request.StoreId))?.Name ?? string.Empty;
        return EntityMapper.ToDto(entity, storeName);
    }

    public override async Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceRequest request)
    {
        var entity = await _deviceRepo.GetByIdAsync(id);
        if (entity is null) return null;

        UpdateEntity(entity, request);
        await _unitOfWork.SaveChangesAsync();

        var storeName = (await _storeRepo.GetByIdAsync(request.StoreId))?.Name ?? string.Empty;
        return EntityMapper.ToDto(entity, storeName);
    }

    public override async Task<List<DeviceDto>> GetAllAsync()
    {
        var entities = await _deviceRepo.GetAllAsync();
        var result = new List<DeviceDto>();
        foreach (var d in entities)
        {
            var storeName = (await _storeRepo.GetByIdAsync(d.StoreId))?.Name ?? string.Empty;
            result.Add(EntityMapper.ToDto(d, storeName));
        }
        return result;
    }

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
