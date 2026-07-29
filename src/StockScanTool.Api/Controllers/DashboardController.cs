using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Authorization;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet]
    [HasPermission("dashboard.read")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary()
        => Ok(ApiResponse<DashboardSummaryDto>.Ok(await _service.GetSummaryAsync()));
}
