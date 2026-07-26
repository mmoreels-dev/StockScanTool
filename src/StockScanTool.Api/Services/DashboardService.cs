using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Api.Services;

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

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var stores = await _storeRepo.CountAsync();
        var products = await _productRepo.CountAsync();
        var devices = await _deviceRepo.CountAsync();

        var allSales = await _saleRepo.GetAllAsync();
        var totalRevenue = allSales.Sum(t => t.TotalAmount);

        var salesByStore = allSales
            .GroupBy(t => new { t.StoreId, t.Store.Name })
            .Select(g => new StoreSalesSummaryDto(
                g.Key.StoreId, g.Key.Name, g.Count(), g.Sum(t => t.TotalAmount)))
            .ToList();

        var stockLevels = (await _inventoryRepo.GetAllAsync())
            .OrderBy(i => i.Store.Name)
            .ThenBy(i => i.Product.Name)
            .Select(i => new StockLevelDto(
                i.Product.Name, i.Product.Barcode,
                i.StoreId, i.Store.Name, i.QuantityOnHand))
            .ToList();

        return new DashboardSummaryDto(stores, products, devices, totalRevenue, salesByStore, stockLevels);
    }
}
