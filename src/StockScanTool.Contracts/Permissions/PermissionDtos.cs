namespace StockScanTool.Contracts;

public record PermissionDto(int Id, string Code, string Name, string Description, string GroupName);
public record CreatePermissionRequest(string Code, string Name, string Description, string GroupName);
public record UpdatePermissionRequest(string Code, string Name, string Description, string GroupName);
