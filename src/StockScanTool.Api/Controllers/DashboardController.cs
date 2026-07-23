using Microsoft.AspNetCore.Mvc;
using StockScanTool.Contracts;
using StockScanTool.Api.Services;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ISaleService _service;

    public DashboardController(ISaleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        => Ok(await _service.GetDashboardAsync());
}
