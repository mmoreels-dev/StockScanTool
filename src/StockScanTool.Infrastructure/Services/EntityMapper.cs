using StockScanTool.Contracts;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Services;

public static class EntityMapper
{
    public static StoreDto ToDto(Store s)
        => new(s.Id, s.Name, s.Address, s.IsActive);

    public static ProductDto ToDto(Product p)
        => new(p.Id, p.Sku, p.Name, p.Description, p.Barcode, p.Price, p.ImagePath);

    public static DeviceDto ToDto(ScanningDevice d, string storeName)
        => new(d.Id, d.DeviceName, d.StoreId, storeName, d.ApiKey, d.IsActive, d.LastPing);

    public static InventoryDto ToDto(Inventory i)
        => new(i.Id, i.ProductId, i.Product.Name, i.Product.Barcode,
               i.StoreId, i.Store.Name, i.QuantityOnHand);

    public static SaleTransactionDto ToDto(SaleTransaction t)
        => new(t.Id, t.StoreId, t.Store.Name,
               t.ScanningDeviceId, t.ScanningDevice.DeviceName,
               t.TotalAmount, t.SaleDate,
               t.SaleItems.Select(ToDto).ToList());

    public static SaleItemDto ToDto(SaleItem si)
        => new(si.Id, si.ProductId, si.Product.Name,
               si.Quantity, si.PriceAtTimeOfSale);
}
