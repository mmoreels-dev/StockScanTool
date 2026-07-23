namespace StockScanTool.Domain.Entities;

public class Inventory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public int QuantityOnHand { get; set; }

    public Product Product { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
