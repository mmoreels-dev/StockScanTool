using Microsoft.AspNetCore.Mvc;
using StockScanTool.Contracts;
using StockScanTool.Api.Services;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _service;

    public StoresController(IStoreService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<StoreDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StoreDto>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto ? Ok(dto) : NotFound();

    [HttpPost]
    public async Task<ActionResult<StoreDto>> Create([FromBody] CreateStoreRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<StoreDto>> Update(int id, [FromBody] UpdateStoreRequest request)
        => await _service.UpdateAsync(id, request) is { } dto ? Ok(dto) : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
