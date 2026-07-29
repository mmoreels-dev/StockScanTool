using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Authorization;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class PermissionsController : BaseController
{
    private readonly IPermissionService _service;

    public PermissionsController(IPermissionService service) => _service = service;

    [HttpGet]
    [HasPermission("roles.read")]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetAll()
        => Ok(ApiResponse<List<PermissionDto>>.Ok(await _service.GetAllAsync()));
}
