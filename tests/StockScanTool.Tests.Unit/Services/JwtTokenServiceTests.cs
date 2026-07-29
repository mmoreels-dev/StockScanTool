using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using StockScanTool.Infrastructure.Services;

namespace StockScanTool.Tests.Unit.Services;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _settings;
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        _settings = new JwtSettings
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationInMinutes = 60
        };
        _sut = new JwtTokenService(Options.Create(_settings));
    }

    [Fact]
    public void GenerateDeviceToken_ReturnsValidToken()
    {
        var token = _sut.GenerateDeviceToken(deviceId: 42, storeId: 7);

        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "42");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Device");
        jwt.Claims.Should().Contain(c => c.Type == "storeId" && c.Value == "7");
        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateAdminToken_ReturnsValidToken()
    {
        var token = _sut.GenerateAdminToken();

        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "admin");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void GenerateDeviceToken_TokenExpiresCorrectly()
    {
        var before = DateTime.UtcNow;
        var token = _sut.GenerateDeviceToken(1, 1);
        var after = DateTime.UtcNow;

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeOnOrBefore(after.AddMinutes(_settings.ExpirationInMinutes).AddSeconds(5));
        jwt.ValidTo.Should().BeAfter(before);
    }

    [Fact]
    public void GenerateDeviceToken_CanBeValidatedWithSameKey()
    {
        var token = _sut.GenerateDeviceToken(1, 1);

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_settings.SecretKey));
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true
        };

        var principal = handler.ValidateToken(token, validationParams, out _);

        principal.Should().NotBeNull();
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("1");
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be("Device");
        principal.FindFirst("storeId")!.Value.Should().Be("1");
    }

    [Fact]
    public void GenerateAdminToken_CanBeValidatedWithSameKey()
    {
        var token = _sut.GenerateAdminToken();

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_settings.SecretKey));
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true
        };

        var principal = handler.ValidateToken(token, validationParams, out _);

        principal.Should().NotBeNull();
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("admin");
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be("Admin");
    }

    [Fact]
    public void GenerateDeviceToken_Throws_WhenSecretKeyEmpty()
    {
        var badSettings = new JwtSettings { SecretKey = "", Issuer = "X", Audience = "X" };
        var sut = new JwtTokenService(Options.Create(badSettings));

        var act = () => sut.GenerateDeviceToken(1, 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretKey*");
    }
}
