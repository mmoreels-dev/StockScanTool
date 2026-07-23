using Microsoft.EntityFrameworkCore;
using StockScanTool.Contracts;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Api.Services;

public class SaleService : ISaleService
{
    private readonly AppDbContext _db;

    public SaleService(AppDbContext db) => _db = db;

    public async Task<SaleTransactionDto?> SubmitSaleAsync(SubmitSaleRequest request)
    {
        if (request.Items is null || request.Items.Count == 0)
            return null;

        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        var productDict = products.ToDictionary(p => p.Id);

        var inventory = await _db.Inventories
            .Where(i => i.StoreId == request.StoreId && productIds.Contains(i.ProductId))
            .ToListAsync();
        var inventoryDict = inventory.ToDictionary(i => i.ProductId);

        foreach (var item in request.Items)
        {
            if (!productDict.ContainsKey(item.ProductId))
                return null;

            if (item.Quantity <= 0)
                return null;

            if (!inventoryDict.TryGetValue(item.ProductId, out var inv) || inv.QuantityOnHand < item.Quantity)
                return null;
        }

        var totalAmount = request.Items.Sum(i => productDict[i.ProductId].Price * i.Quantity);

        var transaction = new Domain.Entities.SaleTransaction
        {
            StoreId = request.StoreId,
            ScanningDeviceId = request.ScanningDeviceId,
            TotalAmount = totalAmount,
            SaleDate = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var product = productDict[item.ProductId];
            transaction.SaleItems.Add(new Domain.Entities.SaleItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                PriceAtTimeOfSale = product.Price
            });

            var inv = inventoryDict[item.ProductId];
            inv.QuantityOnHand -= item.Quantity;
        }

        _db.SaleTransactions.Add(transaction);
        await _db.SaveChangesAsync();

        return await GetTransactionDtoAsync(transaction.Id);
    }

    public async Task<List<SaleTransactionDto>> GetSalesByStoreAsync(int storeId, DateTime? from, DateTime? to)
    {
        var query = _db.SaleTransactions
            .Include(t => t.Store)
            .Include(t => t.ScanningDevice)
            .Include(t => t.SaleItems).ThenInclude(si => si.Product)
            .Where(t => t.StoreId == storeId)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.SaleDate >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.SaleDate <= to.Value);

        return await query.OrderByDescending(t => t.SaleDate)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<List<SaleTransactionDto>> GetAllAsync()
    {
        return await _db.SaleTransactions
            .Include(t => t.Store)
            .Include(t => t.ScanningDevice)
            .Include(t => t.SaleItems).ThenInclude(si => si.Product)
            .OrderByDescending(t => t.SaleDate)
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync()
    {
        var stores = await _db.Stores.CountAsync();
        var products = await _db.Products.CountAsync();
        var devices = await _db.ScanningDevices.CountAsync();

        var transactions = await _db.SaleTransactions
            .Include(t => t.Store)
            .ToListAsync();

        var totalRevenue = transactions.Sum(t => t.TotalAmount);

        var salesByStore = transactions
            .GroupBy(t => new { t.StoreId, t.Store.Name })
            .Select(g => new StoreSalesSummaryDto(
                g.Key.StoreId, g.Key.Name, g.Count(), g.Sum(t => t.TotalAmount)))
            .ToList();

        var stockLevels = await _db.Inventories
            .Include(i => i.Product)
            .Include(i => i.Store)
            .OrderBy(i => i.Store.Name)
            .ThenBy(i => i.Product.Name)
            .Select(i => new StockLevelDto(
                i.Product.Name, i.Product.Barcode,
                i.StoreId, i.Store.Name, i.QuantityOnHand))
            .ToListAsync();

        return new DashboardSummaryDto(stores, products, devices, totalRevenue, salesByStore, stockLevels);
    }

    public async Task<BarcodeLookupResponse?> LookupBarcodeAsync(string barcode, int storeId)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
        if (product is null) return null;

        return new BarcodeLookupResponse(product.Id, product.Sku, product.Name, product.Description, product.Barcode, product.Price);
    }

    private async Task<SaleTransactionDto?> GetTransactionDtoAsync(int transactionId)
    {
        var t = await _db.SaleTransactions
            .Include(x => x.Store)
            .Include(x => x.ScanningDevice)
            .Include(x => x.SaleItems).ThenInclude(si => si.Product)
            .FirstOrDefaultAsync(x => x.Id == transactionId);

        return t is null ? null : MapToDto(t);
    }

    private static SaleTransactionDto MapToDto(Domain.Entities.SaleTransaction t)
    {
        return new SaleTransactionDto(
            t.Id, t.StoreId, t.Store.Name,
            t.ScanningDeviceId, t.ScanningDevice.DeviceName,
            t.TotalAmount, t.SaleDate,
            t.SaleItems.Select(si => new SaleItemDto(
                si.Id, si.ProductId, si.Product.Name,
                si.Quantity, si.PriceAtTimeOfSale)).ToList());
    }
}
