using Microsoft.AspNetCore.Mvc;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseController : ControllerBase
{
}
