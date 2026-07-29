using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Authorization;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class StoresController : BaseController
{
    private readonly IStoreService _service;

    public StoresController(IStoreService service) => _service = service;

    [HttpGet]
    [HasPermission("stores.read")]
    public async Task<ActionResult<ApiResponse<List<StoreDto>>>> GetAll()
        => Ok(ApiResponse<List<StoreDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("paged")]
    [HasPermission("stores.read")]
    public async Task<ActionResult<ApiResponse<PagedResult<StoreDto>>>> GetPaged([FromQuery] PagedRequest request)
        => Ok(ApiResponse<PagedResult<StoreDto>>.Ok(await _service.GetPagedAsync(request)));

    [HttpGet("{id:int}")]
    [HasPermission("stores.read")]
    public async Task<ActionResult<ApiResponse<StoreDto>>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto
            ? Ok(ApiResponse<StoreDto>.Ok(dto))
            : NotFound(ApiResponse<StoreDto>.Fail($"Store with Id={id} not found."));

    [HttpPost]
    [HasPermission("stores.create")]
    public async Task<ActionResult<ApiResponse<StoreDto>>> Create([FromBody] CreateStoreRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<StoreDto>.Ok(dto));
    }

    [HttpPut("{id:int}")]
    [HasPermission("stores.update")]
    public async Task<ActionResult<ApiResponse<StoreDto>>> Update(int id, [FromBody] UpdateStoreRequest request)
        => await _service.UpdateAsync(id, request) is { } dto
            ? Ok(ApiResponse<StoreDto>.Ok(dto))
            : NotFound(ApiResponse<StoreDto>.Fail($"Store with Id={id} not found."));

    [HttpDelete("{id:int}")]
    [HasPermission("stores.delete")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
