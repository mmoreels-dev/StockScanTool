using Microsoft.AspNetCore.Authorization;

namespace StockScanTool.Api.Authorization;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission_";

    public HasPermissionAttribute(string permission)
    {
        Policy = $"{PolicyPrefix}{permission}";
    }
}
