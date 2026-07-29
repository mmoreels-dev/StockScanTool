using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class DeviceService : CrudService<ScanningDevice, DeviceDto, CreateDeviceRequest, UpdateDeviceRequest>, IDeviceService
{
    private readonly IScanningDeviceRepository _deviceRepo;
    private readonly IStoreRepository _storeRepo;

    public DeviceService(
        IScanningDeviceRepository deviceRepo,
        IStoreRepository storeRepo,
        IUnitOfWork unitOfWork,
        IValidator<CreateDeviceRequest> createValidator,
        IValidator<UpdateDeviceRequest> updateValidator)
        : base(deviceRepo, unitOfWork, createValidator, updateValidator)
    {
        _deviceRepo = deviceRepo;
        _storeRepo = storeRepo;
    }

    protected override Expression<Func<ScanningDevice, bool>> IdPredicate(int id)
        => d => d.Id == id;

    protected override DeviceDto ToDto(ScanningDevice d)
        => EntityMapper.ToDto(d, string.Empty);

    protected override ScanningDevice ToEntity(CreateDeviceRequest r)
        => new()
        {
            DeviceName = r.DeviceName,
            StoreId = r.StoreId,
            IsActive = true
        };

    protected override void UpdateEntity(ScanningDevice d, UpdateDeviceRequest r)
    {
        d.DeviceName = r.DeviceName;
        d.StoreId = r.StoreId;
        d.IsActive = r.IsActive;
    }

    public override async Task<DeviceDto> CreateAsync(CreateDeviceRequest request, CancellationToken cancellationToken = default)
    {
        var rawKey = GenerateApiKey();
        var entity = ToEntity(request);
        entity.ApiKey = ApiKeyHasher.Hash(rawKey);
        await _deviceRepo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var storeName = (await _storeRepo.GetByIdAsync(request.StoreId))?.Name ?? string.Empty;
        var dto = EntityMapper.ToDto(entity, storeName);
        return dto with { ApiKey = rawKey };
    }

    public override async Task<DeviceDto?> UpdateAsync(int id, UpdateDeviceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _deviceRepo.GetByIdAsync(id);
        if (entity is null) return null;

        UpdateEntity(entity, request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var storeName = (await _storeRepo.GetByIdAsync(request.StoreId))?.Name ?? string.Empty;
        return EntityMapper.ToDto(entity, storeName);
    }

    public override async Task<List<DeviceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _deviceRepo.GetAllAsync();
        var stores = await LoadStoreNamesAsync(entities);
        return entities.Select(d => EntityMapper.ToDto(d, stores[d.StoreId])).ToList();
    }

    public override async Task<DeviceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _deviceRepo.GetByIdAsync(id);
        if (entity is null) return null;
        var stores = await LoadStoreNamesAsync(new[] { entity });
        return EntityMapper.ToDto(entity, stores[entity.StoreId]);
    }

    public override async Task<PagedResult<DeviceDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _deviceRepo.AsQueryable();

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.Descending
                ? query.OrderByDescending(d => d.DeviceName)
                : query.OrderBy(d => d.DeviceName),
            _ => query.OrderBy(d => d.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var pagedEntities = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var stores = await LoadStoreNamesAsync(pagedEntities);
        return new PagedResult<DeviceDto>(
            pagedEntities.Select(d => EntityMapper.ToDto(d, stores[d.StoreId])).ToList(),
            totalCount, request.Page, request.PageSize);
    }

    private async Task<Dictionary<int, string>> LoadStoreNamesAsync(IEnumerable<ScanningDevice> devices)
    {
        var storeIds = devices.Select(d => d.StoreId).Distinct().ToList();
        var stores = await _storeRepo.FindAsync(s => storeIds.Contains(s.Id));
        return stores.ToDictionary(s => s.Id, s => s.Name);
    }

    private static string GenerateApiKey()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
