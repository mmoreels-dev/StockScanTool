namespace StockScanTool.Contracts;

public record StoreDto(int Id, string Name, string Address, bool IsActive);
public record CreateStoreRequest(string Name, string Address, bool IsActive = true);
public record UpdateStoreRequest(string Name, string Address, bool IsActive);
