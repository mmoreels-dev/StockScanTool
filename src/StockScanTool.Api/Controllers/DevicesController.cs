using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Authorization;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class DevicesController : BaseController
{
    private readonly IDeviceService _service;

    public DevicesController(IDeviceService service) => _service = service;

    [HttpGet]
    [HasPermission("devices.read")]
    public async Task<ActionResult<ApiResponse<List<DeviceDto>>>> GetAll()
        => Ok(ApiResponse<List<DeviceDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("paged")]
    [HasPermission("devices.read")]
    public async Task<ActionResult<ApiResponse<PagedResult<DeviceDto>>>> GetPaged([FromQuery] PagedRequest request)
        => Ok(ApiResponse<PagedResult<DeviceDto>>.Ok(await _service.GetPagedAsync(request)));

    [HttpGet("{id:int}")]
    [HasPermission("devices.read")]
    public async Task<ActionResult<ApiResponse<DeviceDto>>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto
            ? Ok(ApiResponse<DeviceDto>.Ok(dto))
            : NotFound(ApiResponse<DeviceDto>.Fail($"Device with Id={id} not found."));

    [HttpPost]
    [HasPermission("devices.create")]
    public async Task<ActionResult<ApiResponse<DeviceDto>>> Create([FromBody] CreateDeviceRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<DeviceDto>.Ok(dto));
    }

    [HttpPut("{id:int}")]
    [HasPermission("devices.update")]
    public async Task<ActionResult<ApiResponse<DeviceDto>>> Update(int id, [FromBody] UpdateDeviceRequest request)
        => await _service.UpdateAsync(id, request) is { } dto
            ? Ok(ApiResponse<DeviceDto>.Ok(dto))
            : NotFound(ApiResponse<DeviceDto>.Fail($"Device with Id={id} not found."));

    [HttpPost("{id:int}/regenerate-key")]
    [HasPermission("devices.update")]
    public async Task<ActionResult<ApiResponse<DeviceDto>>> RegenerateKey(int id)
        => await _service.RegenerateApiKeyAsync(id) is { } dto
            ? Ok(ApiResponse<DeviceDto>.Ok(dto))
            : NotFound(ApiResponse<DeviceDto>.Fail($"Device with Id={id} not found."));

    [HttpDelete("{id:int}")]
    [HasPermission("devices.delete")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
