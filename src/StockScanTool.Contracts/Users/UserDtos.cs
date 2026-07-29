namespace StockScanTool.Contracts;

public record UserDto(int Id, string Username, string DisplayName, bool IsActive, List<string> Roles);
public record CreateUserRequest(string Username, string Password, string DisplayName, List<int> RoleIds);
public record UpdateUserRequest(string Username, string DisplayName, bool IsActive, List<int> RoleIds);
public record AssignRolesRequest(List<int> RoleIds);
