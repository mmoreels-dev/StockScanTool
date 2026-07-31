using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Authorization;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class PermissionsController : BaseController
{
    private readonly IPermissionService _service;

    public PermissionsController(IPermissionService service) => _service = service;

    [HttpGet]
    [HasPermission("roles.read")]
    public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetAll()
        => Ok(ApiResponse<List<PermissionDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("{id:int}")]
    [HasPermission("roles.read")]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto
            ? Ok(ApiResponse<PermissionDto>.Ok(dto))
            : NotFound(ApiResponse<PermissionDto>.Fail($"Permission with Id={id} not found."));

    [HttpPost]
    [HasPermission("roles.create")]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> Create([FromBody] CreatePermissionRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<PermissionDto>.Ok(dto));
    }

    [HttpPut("{id:int}")]
    [HasPermission("roles.update")]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> Update(int id, [FromBody] UpdatePermissionRequest request)
        => await _service.UpdateAsync(id, request) is { } dto
            ? Ok(ApiResponse<PermissionDto>.Ok(dto))
            : NotFound(ApiResponse<PermissionDto>.Fail($"Permission with Id={id} not found."));

    [HttpDelete("{id:int}")]
    [HasPermission("roles.delete")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
