using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Authorization;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class RolesController : BaseController
{
    private readonly IRoleService _service;

    public RolesController(IRoleService service) => _service = service;

    [HttpGet]
    [HasPermission("roles.read")]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetAll()
        => Ok(ApiResponse<List<RoleDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("paged")]
    [HasPermission("roles.read")]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleDto>>>> GetPaged([FromQuery] PagedRequest request)
        => Ok(ApiResponse<PagedResult<RoleDto>>.Ok(await _service.GetPagedAsync(request)));

    [HttpGet("{id:int}")]
    [HasPermission("roles.read")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto
            ? Ok(ApiResponse<RoleDto>.Ok(dto))
            : NotFound(ApiResponse<RoleDto>.Fail($"Role with Id={id} not found."));

    [HttpPost]
    [HasPermission("roles.create")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create([FromBody] CreateRoleRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<RoleDto>.Ok(dto));
    }

    [HttpPut("{id:int}")]
    [HasPermission("roles.update")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Update(int id, [FromBody] UpdateRoleRequest request)
        => await _service.UpdateAsync(id, request) is { } dto
            ? Ok(ApiResponse<RoleDto>.Ok(dto))
            : NotFound(ApiResponse<RoleDto>.Fail($"Role with Id={id} not found."));

    [HttpDelete("{id:int}")]
    [HasPermission("roles.delete")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
