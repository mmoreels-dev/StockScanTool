using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StockScanTool.Api.Services;

public interface IJwtTokenService
{
    string GenerateDeviceToken(int deviceId, int storeId);
    string GenerateAdminToken();
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public string GenerateDeviceToken(int deviceId, int storeId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, deviceId.ToString()),
            new Claim(ClaimTypes.Role, "Device"),
            new Claim("storeId", storeId.ToString())
        };
        return GenerateToken(claims);
    }

    public string GenerateAdminToken()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        return GenerateToken(claims);
    }

    private string GenerateToken(Claim[] claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured")));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _config.GetValue<int>("JwtSettings:ExpirationInMinutes", 1440)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
