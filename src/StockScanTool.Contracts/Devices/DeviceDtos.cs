namespace StockScanTool.Contracts;

public record DeviceDto(int Id, string DeviceName, int StoreId, string StoreName, string ApiKey, bool IsActive, DateTime? LastPing);
public record CreateDeviceRequest(string DeviceName, int StoreId);
public record UpdateDeviceRequest(string DeviceName, int StoreId, bool IsActive);
public record DeviceLoginRequest(string ApiKey);
public record DeviceLoginResponse(int DeviceId, string DeviceName, int StoreId, string StoreName, string Token);

