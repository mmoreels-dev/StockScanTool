using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Contracts;
using StockScanTool.Api.Services;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _service;

    public DevicesController(IDeviceService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<DeviceDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeviceDto>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto ? Ok(dto) : NotFound();

    [HttpPost]
    public async Task<ActionResult<DeviceDto>> Create([FromBody] CreateDeviceRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DeviceDto>> Update(int id, [FromBody] UpdateDeviceRequest request)
        => await _service.UpdateAsync(id, request) is { } dto ? Ok(dto) : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<DeviceLoginResponse>> Login([FromBody] DeviceLoginRequest request)
        => await _service.LoginAsync(request) is { } response ? Ok(response) : Unauthorized("Invalid or inactive API key");
}
