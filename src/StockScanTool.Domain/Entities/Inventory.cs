namespace StockScanTool.Domain.Entities;

public class Inventory : AuditableEntity
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public int QuantityOnHand { get; set; }
    public long RowVersion { get; set; }

    public Product Product { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
