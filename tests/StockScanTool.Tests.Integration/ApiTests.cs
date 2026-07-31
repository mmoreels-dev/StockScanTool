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
    public async Task AdminLogin_EmptyCredentials_ReturnsBadRequest()
    {
        var request = new { Username = "", Password = "" };
        var response = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/admin-login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeviceLogin_EmptyApiKey_ReturnsBadRequest()
    {
        var request = new { ApiKey = "" };
        var response = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/device-login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    [Fact]
    public async Task ProductLookup_Anonymous_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();
        using var adminClient = _fixture.Factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        var createRequest = new { Sku = "LOOKUP-1", Name = "Lookup Product", Description = "Lookup test", Barcode = "123456789", Price = 19.99m };
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/products", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _fixture.Factory.CreateClient()
            .GetAsync("/api/v1/products/lookup/123456789?storeId=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<BarcodeLookupResponse>>();
        content!.Success.Should().BeTrue();
        content.Data!.Name.Should().Be("Lookup Product");
        content.Data.Barcode.Should().Be("123456789");
    }

    [Fact]
    public async Task ProductLookup_UnknownBarcode_ReturnsNotFound()
    {
        await _fixture.ResetDatabaseAsync();
        var response = await _fixture.Factory.CreateClient()
            .GetAsync("/api/v1/products/lookup/999999999?storeId=1");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

public class DevicesApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public DevicesApiTests(ApiFixture fixture) => _fixture = fixture;

    private HttpClient AuthorizedClient()
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        return client;
    }

    [Fact]
    public async Task CreateDevice_ReturnsApiKey_AndLoginSucceeds()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/devices", new { DeviceName = "scanner-test", StoreId = 1 });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<DeviceDto>>();
        created!.Data!.ApiKey.Should().NotBeNullOrEmpty();

        var loginResponse = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/device-login", new { ApiKey = created.Data.ApiKey });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<DeviceLoginResponse>>();
        login!.Data!.DeviceName.Should().Be("scanner-test");
    }

    [Fact]
    public async Task RegenerateKey_RotatesKey_OldKeyFails_NewKeyWorks()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/devices", new { DeviceName = "scanner-rot", StoreId = 1 });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<DeviceDto>>();
        var deviceId = created!.Data!.Id;
        var oldKey = created.Data.ApiKey!;

        var regenResponse = await client.PostAsync($"/api/v1/devices/{deviceId}/regenerate-key", null);
        regenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var regen = await regenResponse.Content.ReadFromJsonAsync<ApiResponse<DeviceDto>>();
        var newKey = regen!.Data!.ApiKey!;
        newKey.Should().NotBe(oldKey);

        var oldLogin = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/device-login", new { ApiKey = oldKey });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLogin = await _fixture.Factory.CreateClient()
            .PostAsJsonAsync("/api/v1/auth/device-login", new { ApiKey = newKey });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegenerateKey_UnknownDevice_ReturnsNotFound()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var response = await client.PostAsync("/api/v1/devices/9999/regenerate-key", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDevices_DoesNotExposeApiKey()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();
        await client.PostAsJsonAsync("/api/v1/devices", new { DeviceName = "scanner-secret", StoreId = 1 });

        var response = await client.GetAsync("/api/v1/devices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<DeviceDto>>>();
        content!.Data.Should().ContainSingle(d => d.DeviceName == "scanner-secret");
        content.Data.First(d => d.DeviceName == "scanner-secret").ApiKey.Should().BeNullOrEmpty();
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

public class RolesAndPermissionsApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public RolesAndPermissionsApiTests(ApiFixture fixture) => _fixture = fixture;

    private HttpClient AuthorizedClient()
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        return client;
    }

    [Fact]
    public async Task CreatePermission_ReturnsCreated_AndAppearsInList()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/permissions",
            new { Code = "reports.view", Name = "View Reports", Description = "View reports", GroupName = "Reports" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await client.GetAsync("/api/v1/permissions");
        var content = await listResponse.Content.ReadFromJsonAsync<ApiResponse<List<PermissionDto>>>();
        content!.Data.Should().Contain(p => p.Code == "reports.view");
    }

    [Fact]
    public async Task CreatePermission_InvalidCode_ReturnsBadRequest()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var response = await client.PostAsJsonAsync("/api/v1/permissions",
            new { Code = "Not Valid Code!", Name = "Bad", Description = "", GroupName = "Reports" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePermission_DuplicateCode_ReturnsBadRequest()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var first = await client.PostAsJsonAsync("/api/v1/permissions",
            new { Code = "reports.view", Name = "View Reports", Description = "", GroupName = "Reports" });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/api/v1/permissions",
            new { Code = "reports.view", Name = "View Reports Again", Description = "", GroupName = "Reports" });
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePermission_ReturnsOk_WithNewValues()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/permissions",
            new { Code = "reports.view", Name = "View Reports", Description = "", GroupName = "Reports" });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PermissionDto>>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/permissions/{created!.Data!.Id}",
            new { Code = "reports.view", Name = "View Financial Reports", Description = "Updated", GroupName = "Reports" });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<PermissionDto>>();
        updated!.Data!.Name.Should().Be("View Financial Reports");
    }

    [Fact]
    public async Task DeletePermission_AssignedToRole_ReturnsBadRequest()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var response = await client.DeleteAsync("/api/v1/permissions/1");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeletePermission_Unassigned_ReturnsNoContent()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/permissions",
            new { Code = "reports.view", Name = "View Reports", Description = "", GroupName = "Reports" });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PermissionDto>>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/permissions/{created!.Data!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateRole_WithPermissions_ReturnsRoleWithPermissionCodes()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/roles",
            new { Name = "Auditor", Description = "Audit access", PermissionIds = new[] { 1, 2 } });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<RoleDto>>();
        created!.Data!.Permissions.Should().Contain("dashboard.read");

        var getResponse = await client.GetAsync($"/api/v1/roles/{created.Data.Id}");
        var role = await getResponse.Content.ReadFromJsonAsync<ApiResponse<RoleDto>>();
        role!.Data!.Permissions.Should().HaveCount(2);
    }
}

public class UsersApiTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public UsersApiTests(ApiFixture fixture) => _fixture = fixture;

    private HttpClient AuthorizedClient()
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _fixture.AdminToken);
        return client;
    }

    [Fact]
    public async Task CreateUser_ValidRequest_ReturnsCreated()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var response = await client.PostAsJsonAsync("/api/v1/users",
            new { Username = "jdoe", Password = "secret123", DisplayName = "Jane Doe", RoleIds = new[] { 1 } });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        created!.Data!.Username.Should().Be("jdoe");
        created.Data.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ReturnsBadRequest_WithMessage()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var first = await client.PostAsJsonAsync("/api/v1/users",
            new { Username = "jdoe", Password = "secret123", DisplayName = "Jane Doe", RoleIds = new[] { 1 } });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/api/v1/users",
            new { Username = "jdoe", Password = "secret123", DisplayName = "Jane Doe", RoleIds = new[] { 1 } });

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await duplicate.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body!.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateUser_ShortPassword_ReturnsBadRequest_WithValidationMessage()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var response = await client.PostAsJsonAsync("/api/v1/users",
            new { Username = "jdoe", Password = "abc", DisplayName = "Jane Doe", RoleIds = new[] { 1 } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body!.Errors.Should().Contain(e => e.Contains("Password must be at least 6 characters."));
    }

    [Fact]
    public async Task UpdateUser_ValidRequest_ReturnsOk()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = AuthorizedClient();

        var createResponse = await client.PostAsJsonAsync("/api/v1/users",
            new { Username = "jdoe", Password = "secret123", DisplayName = "Jane Doe", RoleIds = new[] { 1 } });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/users/{created!.Data!.Id}",
            new { Username = "jdoe", DisplayName = "Jane Updated", IsActive = true, RoleIds = new[] { 3 } });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        updated!.Data!.DisplayName.Should().Be("Jane Updated");
        updated.Data.Roles.Should().Contain("Viewer");
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

    [Fact]
    public async Task GetPermissions_WithoutToken_Returns401()
    {
        var response = await _fixture.Factory.CreateClient().GetAsync("/api/v1/permissions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}


