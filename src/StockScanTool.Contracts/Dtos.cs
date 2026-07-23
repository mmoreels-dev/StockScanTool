namespace StockScanTool.Contracts;

// --- Store ---
public record StoreDto(int Id, string Name, string Address, bool IsActive);
public record CreateStoreRequest(string Name, string Address, bool IsActive = true);
public record UpdateStoreRequest(string Name, string Address, bool IsActive);

// --- Product ---
public record ProductDto(int Id, string Sku, string Name, string Description, string Barcode, decimal Price);
public record CreateProductRequest(string Sku, string Name, string Description, string Barcode, decimal Price);
public record UpdateProductRequest(string Sku, string Name, string Description, string Barcode, decimal Price);

// --- Device ---
public record DeviceDto(int Id, string DeviceName, int StoreId, string StoreName, string ApiKey, bool IsActive, DateTime? LastPing);
public record CreateDeviceRequest(string DeviceName, int StoreId);
public record UpdateDeviceRequest(string DeviceName, int StoreId, bool IsActive);
public record DeviceLoginRequest(string ApiKey);
public record DeviceLoginResponse(int DeviceId, string DeviceName, int StoreId, string StoreName, string Token);

// --- Inventory ---
public record InventoryDto(int Id, int ProductId, string ProductName, string ProductBarcode, int StoreId, string StoreName, int QuantityOnHand);
public record UpdateInventoryRequest(int ProductId, int StoreId, int QuantityOnHand);

// --- Sale ---
public record SaleTransactionDto(int Id, int StoreId, string StoreName, int ScanningDeviceId, string DeviceName, decimal TotalAmount, DateTime SaleDate, List<SaleItemDto> Items);
public record SaleItemDto(int Id, int ProductId, string ProductName, int Quantity, decimal PriceAtTimeOfSale);
public record SubmitSaleRequest(int StoreId, int ScanningDeviceId, List<SubmitSaleItemRequest> Items);
public record SubmitSaleItemRequest(int ProductId, int Quantity);
public record SaleTransactionResponse(int Id, decimal TotalAmount, DateTime SaleDate, List<SaleItemDto> Items);

// --- Dashboard ---
public record DashboardSummaryDto(int TotalStores, int TotalProducts, int TotalDevices, decimal TotalSalesRevenue, List<StoreSalesSummaryDto> SalesByStore, List<StockLevelDto> StockLevels);
public record StoreSalesSummaryDto(int StoreId, string StoreName, int TransactionCount, decimal TotalRevenue);
public record StockLevelDto(string ProductName, string Barcode, int StoreId, string StoreName, int QuantityOnHand);
public record BarcodeLookupResponse(int Id, string Sku, string Name, string Description, string Barcode, decimal Price);

// --- Cart (shared across scanner clients) ---
public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Total => Price * Quantity;
}
