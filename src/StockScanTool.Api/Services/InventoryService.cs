using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Api.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IInventoryRepository inventoryRepo, IUnitOfWork unitOfWork)
    {
        _inventoryRepo = inventoryRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<InventoryDto>> GetAllAsync()
    {
        var entities = await _inventoryRepo.GetAllAsync();
        return entities.Select(EntityMapper.ToDto).ToList();
    }

    public async Task<List<InventoryDto>> GetByStoreAsync(int storeId)
    {
        var entities = await _inventoryRepo.GetByStoreAsync(storeId);
        return entities.Select(EntityMapper.ToDto).ToList();
    }

    public async Task<InventoryDto?> GetByIdAsync(int id)
    {
        var entity = await _inventoryRepo.GetByIdAsync(id);
        return entity is null ? null : EntityMapper.ToDto(entity);
    }

    public async Task<InventoryDto> UpsertAsync(UpdateInventoryRequest request)
    {
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

        await _unitOfWork.SaveChangesAsync();
        var saved = await _inventoryRepo.GetByProductAndStoreAsync(request.ProductId, request.StoreId);
        return EntityMapper.ToDto(saved!);
    }

    public async Task<bool> DecrementStockAsync(int productId, int storeId, int quantity)
    {
        var inventory = await _inventoryRepo.GetByProductAndStoreAsync(productId, storeId);
        if (inventory is null || inventory.QuantityOnHand < quantity)
            return false;

        inventory.QuantityOnHand -= quantity;
        _inventoryRepo.Update(inventory);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
