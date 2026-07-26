namespace StockScanTool.Contracts;

public record SaleTransactionDto(int Id, int StoreId, string StoreName, int ScanningDeviceId, string DeviceName, decimal TotalAmount, DateTime SaleDate, List<SaleItemDto> Items);
public record SaleItemDto(int Id, int ProductId, string ProductName, int Quantity, decimal PriceAtTimeOfSale);
public record SubmitSaleRequest(int StoreId, int ScanningDeviceId, List<SubmitSaleItemRequest> Items);
public record SubmitSaleItemRequest(int ProductId, int Quantity);
