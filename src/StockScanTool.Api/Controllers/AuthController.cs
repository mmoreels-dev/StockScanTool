using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Services;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IJwtTokenService _jwt;

    public AuthController(AuthService authService, IJwtTokenService jwt)
    {
        _authService = authService;
        _jwt = jwt;
    }

    [HttpPost("device-login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DeviceLoginResponse>>> DeviceLogin([FromBody] DeviceLoginRequest request)
        => await _authService.LoginAsync(request) is { } response
            ? Ok(ApiResponse<DeviceLoginResponse>.Ok(response))
            : Unauthorized(ApiResponse<DeviceLoginResponse>.Fail("Invalid or inactive API key."));

    [HttpPost("admin-login")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<AdminLoginResponse>> AdminLogin([FromBody] AdminLoginRequest request)
    {
        if (request.Username != "admin" || request.Password != "admin")
            return Unauthorized(ApiResponse<AdminLoginResponse>.Fail("Invalid credentials."));

        var token = _jwt.GenerateAdminToken();
        return Ok(ApiResponse<AdminLoginResponse>.Ok(new AdminLoginResponse(token)));
    }
}
