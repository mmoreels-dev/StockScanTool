using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("device-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<DeviceLoginResponse>>> DeviceLogin([FromBody] DeviceLoginRequest request)
        => await _authService.LoginDeviceAsync(request) is { } response
            ? Ok(ApiResponse<DeviceLoginResponse>.Ok(response))
            : Unauthorized(ApiResponse<DeviceLoginResponse>.Fail("Invalid or inactive API key."));

    [HttpPost("admin-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AdminLoginResponse>>> AdminLogin([FromBody] AdminLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ApiResponse<AdminLoginResponse>.Fail("Username and password are required."));

        var result = await _authService.LoginUserAsync(request.Username, request.Password);
        if (result is null)
            return Unauthorized(ApiResponse<AdminLoginResponse>.Fail("Invalid credentials."));

        return Ok(ApiResponse<AdminLoginResponse>.Ok(result));
    }
}
