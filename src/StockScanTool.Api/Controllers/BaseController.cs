using Microsoft.AspNetCore.Mvc;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected ActionResult<T> HandleResult<T>(Application.Results.Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Data);
        return NotFound(ApiResponse<T>.Fail(result.Errors));
    }
}
