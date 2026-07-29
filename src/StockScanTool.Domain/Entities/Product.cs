namespace StockScanTool.Domain.Entities;

public class Product : AuditableEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImagePath { get; set; }

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
