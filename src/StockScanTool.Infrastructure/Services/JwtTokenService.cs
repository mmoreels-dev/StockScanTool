using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using StockScanTool.Application.Services;

namespace StockScanTool.Infrastructure.Services;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; } = 1440;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public int ExpirationInMinutes => _settings.ExpirationInMinutes;

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

    public string GenerateUserToken(int userId, string username, string displayName, IList<string> roles, IList<string> permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim("displayName", displayName)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        return GenerateToken([.. claims]);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private string GenerateToken(Claim[] claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            string.IsNullOrEmpty(_settings.SecretKey)
                ? throw new InvalidOperationException("JWT SecretKey not configured.")
                : _settings.SecretKey));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationInMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
