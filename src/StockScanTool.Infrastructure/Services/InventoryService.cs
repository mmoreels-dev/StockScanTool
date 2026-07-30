using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateInventoryRequest> _validator;

    public InventoryService(
        IInventoryRepository inventoryRepo,
        IUnitOfWork unitOfWork,
        IValidator<UpdateInventoryRequest> validator)
    {
        _inventoryRepo = inventoryRepo;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PagedResult<InventoryDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        IQueryable<Inventory> query = _inventoryRepo.AsQueryable()
            .Include(i => i.Product)
            .Include(i => i.Store);

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "product" => request.Descending
                ? query.OrderByDescending(i => i.Product.Name)
                : query.OrderBy(i => i.Product.Name),
            "store" => request.Descending
                ? query.OrderByDescending(i => i.Store.Name)
                : query.OrderBy(i => i.Store.Name),
            "quantity" => request.Descending
                ? query.OrderByDescending(i => i.QuantityOnHand)
                : query.OrderBy(i => i.QuantityOnHand),
            _ => query.OrderBy(i => i.Store.Name).ThenBy(i => i.Product.Name)
        };

        var paged = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryDto>(
            paged.Select(EntityMapper.ToDto).ToList(), totalCount, request.Page, request.PageSize);
    }

    public async Task<List<InventoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _inventoryRepo.GetAllAsync();
        return entities.Select(EntityMapper.ToDto).ToList();
    }

    public async Task<List<InventoryDto>> GetByStoreAsync(int storeId, CancellationToken cancellationToken = default)
    {
        var entities = await _inventoryRepo.GetByStoreAsync(storeId);
        return entities.Select(EntityMapper.ToDto).ToList();
    }

    public async Task<InventoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _inventoryRepo.GetByIdAsync(id);
        return entity is null ? null : EntityMapper.ToDto(entity);
    }

    public async Task<InventoryDto> UpsertAsync(UpdateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existing = await _inventoryRepo.GetByProductAndStoreAsync(request.ProductId, request.StoreId);

        if (existing is not null)
        {
            existing.QuantityOnHand = request.QuantityOnHand;
            _inventoryRepo.Update(existing);
        }
        else
        {
            existing = new Inventory
            {
                ProductId = request.ProductId,
                StoreId = request.StoreId,
                QuantityOnHand = request.QuantityOnHand
            };
            await _inventoryRepo.AddAsync(existing);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The inventory record has been modified by another transaction. Please refresh and try again.");
        }

        var saved = await _inventoryRepo.GetByProductAndStoreAsync(request.ProductId, request.StoreId);
        return EntityMapper.ToDto(saved!);
    }

    public async Task<bool> DecrementStockAsync(int productId, int storeId, int quantity, CancellationToken cancellationToken = default)
    {
        var inventory = await _inventoryRepo.GetByProductAndStoreAsync(productId, storeId);
        if (inventory is null || inventory.QuantityOnHand < quantity)
            return false;

        inventory.QuantityOnHand -= quantity;
        _inventoryRepo.Update(inventory);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
