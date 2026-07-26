using StockScanTool.Application.Repositories;
using StockScanTool.Application.Results;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Api.Services;

public class SaleService : ISaleService
{
    private readonly ISaleTransactionRepository _saleRepo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SaleService(
        ISaleTransactionRepository saleRepo,
        IProductRepository productRepo,
        IInventoryRepository inventoryRepo,
        IUnitOfWork unitOfWork)
    {
        _saleRepo = saleRepo;
        _productRepo = productRepo;
        _inventoryRepo = inventoryRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SaleTransactionDto>> SubmitSaleAsync(SubmitSaleRequest request)
    {
        if (request.Items is null || request.Items.Count == 0)
            return Result<SaleTransactionDto>.Fail("Sale cannot be submitted with an empty cart.");

        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepo.FindAsync(p => productIds.Contains(p.Id));
        var productDict = products.ToDictionary(p => p.Id);

        var inventory = await _inventoryRepo.FindAsync(
            i => i.StoreId == request.StoreId && productIds.Contains(i.ProductId));
        var inventoryDict = inventory.ToDictionary(i => i.ProductId);

        var errors = new List<string>();

        foreach (var item in request.Items)
        {
            if (!productDict.ContainsKey(item.ProductId))
                errors.Add($"Product with Id={item.ProductId} not found.");

            if (item.Quantity <= 0)
                errors.Add($"Quantity must be positive for product Id={item.ProductId}.");

            if (inventoryDict.TryGetValue(item.ProductId, out var inv) && inv.QuantityOnHand < item.Quantity)
                errors.Add($"Insufficient stock for product Id={item.ProductId}: requested {item.Quantity}, available {inv.QuantityOnHand}.");
            else if (!inventoryDict.ContainsKey(item.ProductId))
                errors.Add($"No inventory record for product Id={item.ProductId} in store Id={request.StoreId}.");
        }

        if (errors.Count > 0)
            return Result<SaleTransactionDto>.Fail(errors);

        var totalAmount = request.Items.Sum(i => productDict[i.ProductId].Price * i.Quantity);

        var transaction = new SaleTransaction
        {
            StoreId = request.StoreId,
            ScanningDeviceId = request.ScanningDeviceId,
            TotalAmount = totalAmount,
            SaleDate = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var product = productDict[item.ProductId];
            transaction.SaleItems.Add(new SaleItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                PriceAtTimeOfSale = product.Price
            });

            var inv = inventoryDict[item.ProductId];
            inv.QuantityOnHand -= item.Quantity;
            _inventoryRepo.Update(inv);
        }

        await _saleRepo.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return Result<SaleTransactionDto>.Ok(EntityMapper.ToDto(transaction));
    }

    public async Task<List<SaleTransactionDto>> GetAllAsync()
    {
        var transactions = await _saleRepo.GetAllAsync();
        return transactions
            .OrderByDescending(t => t.SaleDate)
            .Select(EntityMapper.ToDto)
            .ToList();
    }

    public async Task<List<SaleTransactionDto>> GetByStoreAsync(int storeId, DateTime? from, DateTime? to)
    {
        var transactions = await _saleRepo.GetByStoreWithDateFilterAsync(storeId, from, to);
        return transactions.Select(EntityMapper.ToDto).ToList();
    }
}
