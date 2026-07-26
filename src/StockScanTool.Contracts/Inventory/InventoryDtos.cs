namespace StockScanTool.Contracts;

public record InventoryDto(int Id, int ProductId, string ProductName, string ProductBarcode, int StoreId, string StoreName, int QuantityOnHand);
public record UpdateInventoryRequest(int ProductId, int StoreId, int QuantityOnHand);
