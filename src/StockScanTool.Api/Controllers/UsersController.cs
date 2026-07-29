using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Authorization;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class UsersController : BaseController
{
    private readonly IUserService _service;

    public UsersController(IUserService service) => _service = service;

    [HttpGet]
    [HasPermission("users.read")]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAll()
        => Ok(ApiResponse<List<UserDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("{id:int}")]
    [HasPermission("users.read")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto
            ? Ok(ApiResponse<UserDto>.Ok(dto))
            : NotFound(ApiResponse<UserDto>.Fail($"User with Id={id} not found."));

    [HttpPost]
    [HasPermission("users.create")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<UserDto>.Ok(dto));
    }

    [HttpPut("{id:int}")]
    [HasPermission("users.update")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(int id, [FromBody] UpdateUserRequest request)
        => await _service.UpdateAsync(id, request) is { } dto
            ? Ok(ApiResponse<UserDto>.Ok(dto))
            : NotFound(ApiResponse<UserDto>.Fail($"User with Id={id} not found."));

    [HttpDelete("{id:int}")]
    [HasPermission("users.delete")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
