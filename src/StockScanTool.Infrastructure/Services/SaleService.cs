using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Results;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class SaleService : ISaleService
{
    private readonly ISaleTransactionRepository _saleRepo;
    private readonly IProductRepository _productRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SubmitSaleRequest> _validator;
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        ISaleTransactionRepository saleRepo,
        IProductRepository productRepo,
        IInventoryRepository inventoryRepo,
        IUnitOfWork unitOfWork,
        IValidator<SubmitSaleRequest> validator,
        ILogger<SaleService> logger)
    {
        _saleRepo = saleRepo;
        _productRepo = productRepo;
        _inventoryRepo = inventoryRepo;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<SaleTransactionDto>> SubmitSaleAsync(SubmitSaleRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<SaleTransactionDto>.Fail(validation.Errors.Select(e => e.ErrorMessage).ToList());

        if (request.Items is null || request.Items.Count == 0)
            return Result<SaleTransactionDto>.Fail("Sale cannot be submitted with an empty cart.");

        _logger.LogInformation("Processing sale for StoreId={StoreId}, DeviceId={DeviceId}, Items={ItemCount}",
            request.StoreId, request.ScanningDeviceId, request.Items.Count);

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

        try
        {
            await _saleRepo.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict during sale submission for StoreId={StoreId}", request.StoreId);
            return Result<SaleTransactionDto>.Fail("The inventory has been modified by another transaction. Please retry.");
        }

        var saved = await _saleRepo.GetByIdWithIncludesAsync(transaction.Id)
            ?? throw new InvalidOperationException($"Sale transaction {transaction.Id} could not be retrieved after save.");

        return Result<SaleTransactionDto>.Ok(EntityMapper.ToDto(saved));
    }

    public async Task<PagedResult<SaleTransactionDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        IQueryable<SaleTransaction> query = _saleRepo.AsQueryable()
            .Include(s => s.Store)
            .Include(s => s.ScanningDevice)
            .Include(s => s.SaleItems).ThenInclude(i => i.Product);

        var totalCount = await query.CountAsync(cancellationToken);

        query = request.SortBy?.ToLower() switch
        {
            "date" => request.Descending
                ? query.OrderByDescending(s => s.SaleDate)
                : query.OrderBy(s => s.SaleDate),
            "store" => request.Descending
                ? query.OrderByDescending(s => s.Store.Name)
                : query.OrderBy(s => s.Store.Name),
            "amount" => request.Descending
                ? query.OrderByDescending(s => s.TotalAmount)
                : query.OrderBy(s => s.TotalAmount),
            _ => query.OrderByDescending(s => s.SaleDate)
        };

        var paged = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SaleTransactionDto>(
            paged.Select(EntityMapper.ToDto).ToList(), totalCount, request.Page, request.PageSize);
    }

    public async Task<List<SaleTransactionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var transactions = await _saleRepo.GetAllAsync();
        return transactions
            .OrderByDescending(t => t.SaleDate)
            .Select(EntityMapper.ToDto)
            .ToList();
    }

    public async Task<List<SaleTransactionDto>> GetByStoreAsync(int storeId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var transactions = await _saleRepo.GetByStoreWithDateFilterAsync(storeId, from, to);
        return transactions.Select(EntityMapper.ToDto).ToList();
    }
}
