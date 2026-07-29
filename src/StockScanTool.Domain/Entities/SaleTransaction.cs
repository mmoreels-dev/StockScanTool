namespace StockScanTool.Domain.Entities;

public class SaleTransaction : AuditableEntity
{
    public int StoreId { get; set; }
    public int ScanningDeviceId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public Store Store { get; set; } = null!;
    public ScanningDevice ScanningDevice { get; set; } = null!;
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
