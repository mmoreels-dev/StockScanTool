using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;
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
    private readonly string _dbName = Guid.NewGuid().ToString();

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
                        options.UseInMemoryDatabase(_dbName)
                               .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
                });
            });

        var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();

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
            new(ClaimTypes.NameIdentifier, "1"),
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

    public async Task ResetDatabaseAsync()
    {
        var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await SeedData.InitializeAsync(context, passwordHasher);
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();
    }
}

public class AuthApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public AuthApiTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task AdminLogin_ValidCredentials_ReturnsToken()
    {
        var request = new { Username = "admin", Password = "admin" };
        var response = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/admin-login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<AdminLoginResponse>>();
        content!.Success.Should().BeTrue();
        content.Data!.Token.Should().NotBeNullOrEmpty();
        content.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AdminLogin_InvalidCredentials_ReturnsUnauthorized()
    {
        var request = new { Username = "admin", Password = "wrong" };
        var response = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/admin-login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        var loginRequest = new { Username = "admin", Password = "admin" };
        var loginResponse = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/admin-login", loginRequest);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AdminLoginResponse>>();

        var refreshRequest = new { RefreshToken = loginContent!.Data!.RefreshToken };
        var response = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>();
        content!.Success.Should().BeTrue();
        content.Data!.Token.Should().NotBeNullOrEmpty();
        content.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ReturnsUnauthorized()
    {
        var request = new { RefreshToken = "invalid-refresh-token" };
        var response = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/refresh", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class StoresApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public StoresApiTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetStores_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/stores");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Downtown Branch");
    }

    [Fact]
    public async Task GetStoresPaged_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/stores/paged?Page=1&PageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<StoreDto>>>();
        content!.Success.Should().BeTrue();
        content.Data!.Items.Should().NotBeEmpty();
        content.Data.TotalCount.Should().Be(6);
    }

    [Fact]
    public async Task GetStore_ById_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/stores/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStore_ByInvalidId_ReturnsNotFound()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/stores/999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateStore_ReturnsCreated()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var request = new { Name = "New Store", Address = "789 New Rd", IsActive = true };
        var response = await client.PostAsJsonAsync("/api/v1/stores", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdateStore_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var request = new { Name = "Updated Store", Address = "123 Updated St", IsActive = true };
        var response = await client.PutAsJsonAsync("/api/v1/stores/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteStore_ReturnsNoContent()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var request = new { Name = "Delete Me", Address = "Temp", IsActive = true };
        var createResponse = await client.PostAsJsonAsync("/api/v1/stores", request);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<StoreDto>>();

        var response = await client.DeleteAsync($"/api/v1/stores/{created!.Data!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

public class ProductsApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public ProductsApiTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProductsPaged_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var response = await client.GetAsync("/api/v1/products/paged?Page=1&PageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreated()
    {
        await _fixture.ResetDatabaseAsync();
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
        await _fixture.ResetDatabaseAsync();
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

public class UnauthorizedAccessTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public UnauthorizedAccessTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetStores_WithoutToken_Returns401()
    {
        var response = await _fixture.Factory.CreateClient().GetAsync("/api/v1/stores");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_WithoutToken_Returns401()
    {
        var response = await _fixture.Factory.CreateClient().GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}


