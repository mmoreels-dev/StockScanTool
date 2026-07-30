using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IRepository<Store> _storeRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<ScanningDevice> _deviceRepo;
    private readonly ISaleTransactionRepository _saleRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public DashboardService(
        IRepository<Store> storeRepo,
        IRepository<Product> productRepo,
        IRepository<ScanningDevice> deviceRepo,
        ISaleTransactionRepository saleRepo,
        IInventoryRepository inventoryRepo)
    {
        _storeRepo = storeRepo;
        _productRepo = productRepo;
        _deviceRepo = deviceRepo;
        _saleRepo = saleRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var stores = await _storeRepo.CountAsync();
        var products = await _productRepo.CountAsync();
        var devices = await _deviceRepo.CountAsync();

        var totalRevenue = await _saleRepo.AsQueryable()
            .SumAsync(s => s.TotalAmount, cancellationToken);

        var salesByStore = await _saleRepo.AsQueryable()
            .GroupBy(s => new { s.StoreId, s.Store.Name })
            .Select(g => new StoreSalesSummaryDto(
                g.Key.StoreId, g.Key.Name, g.Count(), g.Sum(s => s.TotalAmount)))
            .ToListAsync(cancellationToken);

        var stockLevels = await _inventoryRepo.AsQueryable()
            .OrderBy(i => i.Store.Name)
            .ThenBy(i => i.Product.Name)
            .Select(i => new StockLevelDto(
                i.Product.Name, i.Product.Barcode,
                i.StoreId, i.Store.Name, i.QuantityOnHand))
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto(stores, products, devices, totalRevenue, salesByStore, stockLevels);
    }
}
