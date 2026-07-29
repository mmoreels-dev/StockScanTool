namespace StockScanTool.Contracts;

public record RoleDto(int Id, string Name, string Description, bool IsActive, List<string> Permissions);
public record CreateRoleRequest(string Name, string Description, List<int> PermissionIds);
public record UpdateRoleRequest(string Name, string Description, bool IsActive, List<int> PermissionIds);
public record AssignPermissionsRequest(List<int> PermissionIds);
