namespace StockScanTool.Contracts;

public record DashboardSummaryDto(int TotalStores, int TotalProducts, int TotalDevices, decimal TotalSalesRevenue, List<StoreSalesSummaryDto> SalesByStore, List<StockLevelDto> StockLevels);
public record StoreSalesSummaryDto(int StoreId, string StoreName, int TransactionCount, decimal TotalRevenue);
public record StockLevelDto(string ProductName, string Barcode, int StoreId, string StoreName, int QuantityOnHand);
public record BarcodeLookupResponse(int Id, string Sku, string Name, string Description, string Barcode, decimal Price);
