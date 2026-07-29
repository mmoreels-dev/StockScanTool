using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IRepository<Domain.Entities.Store> _storeRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<Domain.Entities.ScanningDevice> _deviceRepo;
    private readonly ISaleTransactionRepository _saleRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public DashboardService(
        IRepository<Domain.Entities.Store> storeRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<Domain.Entities.ScanningDevice> deviceRepo,
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
