using FluentValidation;
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
    private readonly IValidator<AdminLoginRequest> _adminLoginValidator;
    private readonly IValidator<DeviceLoginRequest> _deviceLoginValidator;

    public AuthController(
        IAuthService authService,
        IValidator<AdminLoginRequest> adminLoginValidator,
        IValidator<DeviceLoginRequest> deviceLoginValidator)
    {
        _authService = authService;
        _adminLoginValidator = adminLoginValidator;
        _deviceLoginValidator = deviceLoginValidator;
    }

    [HttpPost("device-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<DeviceLoginResponse>>> DeviceLogin([FromBody] DeviceLoginRequest request)
    {
        await _deviceLoginValidator.ValidateAndThrowAsync(request);
        return await _authService.LoginDeviceAsync(request) is { } response
            ? Ok(ApiResponse<DeviceLoginResponse>.Ok(response))
            : Unauthorized(ApiResponse<DeviceLoginResponse>.Fail("Invalid or inactive API key."));
    }

    [HttpPost("admin-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<AdminLoginResponse>>> AdminLogin([FromBody] AdminLoginRequest request)
    {
        await _adminLoginValidator.ValidateAndThrowAsync(request);

        var result = await _authService.LoginUserAsync(request.Username, request.Password);
        if (result is null)
            return Unauthorized(ApiResponse<AdminLoginResponse>.Fail("Invalid credentials."));

        return Ok(ApiResponse<AdminLoginResponse>.Ok(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(ApiResponse<RefreshTokenResponse>.Fail("Refresh token is required."));

        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (result is null)
            return Unauthorized(ApiResponse<RefreshTokenResponse>.Fail("Invalid or expired refresh token."));

        return Ok(ApiResponse<RefreshTokenResponse>.Ok(result));
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> Revoke([FromBody] RevokeRefreshTokenRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userId = userIdClaim is not null ? int.Parse(userIdClaim) : (int?)null;

        var result = await _authService.RevokeRefreshTokenAsync(request.RefreshToken, userId);
        if (!result)
            return NotFound(ApiResponse<string>.Fail("No active refresh tokens found to revoke."));

        return Ok(ApiResponse<string>.Ok("Refresh token(s) revoked successfully."));
    }
}
