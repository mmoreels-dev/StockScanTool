namespace StockScanTool.Domain.Entities;

public class ScanningDevice : AuditableEntity
{
    public string DeviceName { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastPing { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<SaleTransaction> SaleTransactions { get; set; } = new List<SaleTransaction>();
}
