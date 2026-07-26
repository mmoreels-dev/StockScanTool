using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class InventoryController : BaseController
{
    private readonly IInventoryService _service;

    public InventoryController(IInventoryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InventoryDto>>>> GetAll()
        => Ok(ApiResponse<List<InventoryDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("store/{storeId:int}")]
    public async Task<ActionResult<ApiResponse<List<InventoryDto>>>> GetByStore(int storeId)
        => Ok(ApiResponse<List<InventoryDto>>.Ok(await _service.GetByStoreAsync(storeId)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto
            ? Ok(ApiResponse<InventoryDto>.Ok(dto))
            : NotFound(ApiResponse<InventoryDto>.Fail($"Inventory record with Id={id} not found."));

    [HttpPut]
    public async Task<ActionResult<ApiResponse<InventoryDto>>> Upsert([FromBody] UpdateInventoryRequest request)
        => Ok(ApiResponse<InventoryDto>.Ok(await _service.UpsertAsync(request)));
}
