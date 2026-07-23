using Microsoft.AspNetCore.Mvc;
using StockScanTool.Contracts;
using StockScanTool.Api.Services;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<InventoryDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("store/{storeId:int}")]
    public async Task<ActionResult<List<InventoryDto>>> GetByStore(int storeId)
        => Ok(await _service.GetByStoreAsync(storeId));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryDto>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto ? Ok(dto) : NotFound();

    [HttpPost]
    public async Task<ActionResult<InventoryDto>> Upsert([FromBody] UpdateInventoryRequest request)
        => Ok(await _service.UpsertAsync(request));
}
