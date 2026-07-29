using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Data;
using StockScanTool.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace StockScanTool.Tests.Integration;

public class ApiFixture : IAsyncLifetime
{
    private const string TestJwtKey = "IntegrationTestSecretKeyThatIsDefinitelyLongEnoughForHmac256!!";

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string AdminToken { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", TestJwtKey);

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(
                        d => d.ServiceType.FullName != null &&
                            d.ServiceType.FullName.Contains("AppDbContext"))
                        .ToList();

                    foreach (var descriptor in descriptors)
                        services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("IntegrationTestDb")
                               .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
                });
            });

        var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

        if (!await context.Stores.AnyAsync())
        {
            context.Stores.AddRange(
                new Store { Name = "Downtown Branch", Address = "123 Main St", IsActive = true },
                new Store { Name = "Mall Location", Address = "456 Commerce Ave", IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        AdminToken = GenerateTestToken();
    }

    private static string GenerateTestToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var allPermissions = new[]
        {
            "dashboard.read",
            "stores.read", "stores.create", "stores.update", "stores.delete",
            "products.read", "products.create", "products.update", "products.delete",
            "devices.read", "devices.create", "devices.update", "devices.delete",
            "inventory.read", "inventory.create", "inventory.update",
            "sales.read", "sales.create",
            "users.read", "users.create", "users.update", "users.delete",
            "roles.read", "roles.create", "roles.update", "roles.delete",
            "scanning.sell"
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "admin"),
            new(ClaimTypes.Role, "Admin"),
            new("displayName", "Admin User"),
        };
        claims.AddRange(allPermissions.Select(p => new Claim("permission", p)));
        var token = new JwtSecurityToken(
            issuer: "StockScanTool",
            audience: "StockScanToolApp",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", null);
        Factory?.Dispose();
        await Task.CompletedTask;
    }
}

public class StoresApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public StoresApiTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetStores_ReturnsOk()
    {
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/stores");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Downtown Branch");
    }

    [Fact]
    public async Task GetStore_ById_ReturnsOk()
    {
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/stores/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStore_ByInvalidId_ReturnsNotFound()
    {
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/stores/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public class ProductsApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public ProductsApiTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreated()
    {
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var request = new { Sku = "TEST-1", Name = "Test Product", Description = "A test", Barcode = "999999", Price = 9.99m };
        var response = await client.PostAsJsonAsync("/api/v1/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_InvalidData_ReturnsBadRequest()
    {
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var request = new { Sku = "", Name = "", Description = "", Barcode = "", Price = -1m };
        var response = await client.PostAsJsonAsync("/api/v1/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

public class DashboardApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public DashboardApiTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetDashboard_Unauthorized_Returns401()
    {
        var response = await _fixture.Factory.CreateClient().GetAsync("/api/v1/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
