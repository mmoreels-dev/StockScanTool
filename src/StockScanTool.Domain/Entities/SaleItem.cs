namespace StockScanTool.Domain.Entities;

public class SaleItem
{
    public int Id { get; set; }
    public int SaleTransactionId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtTimeOfSale { get; set; }

    public SaleTransaction SaleTransaction { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
