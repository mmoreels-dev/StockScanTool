namespace StockScanTool.Domain.Entities;

public class Store : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ScanningDevice> ScanningDevices { get; set; } = new List<ScanningDevice>();
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<SaleTransaction> SaleTransactions { get; set; } = new List<SaleTransaction>();
}
