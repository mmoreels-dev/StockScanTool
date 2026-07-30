using System.Net.Http.Json;
using StockScanTool.Contracts;
using StockScanTool.Shared.Services;

namespace StockScanTool.Web.Services;

public class ApiClient : BaseApiService
{
    private readonly AuthStateService _auth;
    private bool _tokenAttached;

    public ApiClient(HttpClient http, AuthStateService auth) : base(http)
    {
        _auth = auth;
        var baseUrl = http.BaseAddress?.ToString();
        if (!string.IsNullOrEmpty(baseUrl))
            SetBaseUrl(baseUrl);
    }

    protected override async Task EnsureAuthenticatedAsync()
    {
        if (_tokenAttached && _auth.IsAuthenticated) return;

        await _auth.InitializeAsync();

        if (_auth.IsAuthenticated && !string.IsNullOrEmpty(_auth.Token))
        {
            _token = _auth.Token;
            AttachToken();
            _tokenAttached = true;
        }
        else
        {
            ClearToken();
            _tokenAttached = false;
        }
    }

    protected override async Task OnUnauthorizedAsync()
    {
        await _auth.LogoutAsync();
        _token = string.Empty;
        _tokenAttached = false;
        ClearToken();
    }

    public void SetToken(string token)
    {
        _auth.SetToken(token);
        _token = token;
        AttachToken();
        _tokenAttached = true;
    }

    // ── Stores ──────────────────────────────────────────
    public async Task<List<StoreDto>> GetStores()
        => await GetAsync<List<StoreDto>>(ApiRoutes.Stores.Base) ?? [];

    public async Task<StoreDto?> GetStore(int id)
        => await GetAsync<StoreDto>($"{ApiRoutes.Stores.Base}/{id}");

    public async Task<StoreDto> CreateStore(CreateStoreRequest r)
        => (await PostAsync<StoreDto, CreateStoreRequest>(ApiRoutes.Stores.Base, r))!;

    public async Task<StoreDto?> UpdateStore(int id, UpdateStoreRequest r)
        => await PutAsync<StoreDto, UpdateStoreRequest>($"{ApiRoutes.Stores.Base}/{id}", r);

    public async Task DeleteStore(int id)
        => await DeleteAsync($"{ApiRoutes.Stores.Base}/{id}");

    // ── Products ────────────────────────────────────────
    public async Task<List<ProductDto>> GetProducts()
        => await GetAsync<List<ProductDto>>(ApiRoutes.Products.Base) ?? [];

    public async Task<ProductDto?> GetProduct(int id)
        => await GetAsync<ProductDto>($"{ApiRoutes.Products.Base}/{id}");

    public async Task<ProductDto> CreateProduct(CreateProductRequest r)
        => (await PostAsync<ProductDto, CreateProductRequest>(ApiRoutes.Products.Base, r))!;

    public async Task<ProductDto?> UpdateProduct(int id, UpdateProductRequest r)
        => await PutAsync<ProductDto, UpdateProductRequest>($"{ApiRoutes.Products.Base}/{id}", r);

    public async Task DeleteProduct(int id)
        => await DeleteAsync($"{ApiRoutes.Products.Base}/{id}");

    public async Task<ProductDto> UploadProductImage(int productId, Stream imageStream, string fileName)
        => (await UploadAsync<ProductDto>($"{ApiRoutes.Products.Base}/{productId}/image", imageStream, fileName))!;

    public async Task DeleteProductImage(int productId)
        => await DeleteAsync($"{ApiRoutes.Products.Base}/{productId}/image");

    // ── Devices ─────────────────────────────────────────
    public async Task<List<DeviceDto>> GetDevices()
        => await GetAsync<List<DeviceDto>>(ApiRoutes.Devices.Base) ?? [];

    public async Task<DeviceDto?> GetDevice(int id)
        => await GetAsync<DeviceDto>($"{ApiRoutes.Devices.Base}/{id}");

    public async Task<DeviceDto> CreateDevice(CreateDeviceRequest r)
        => (await PostAsync<DeviceDto, CreateDeviceRequest>(ApiRoutes.Devices.Base, r))!;

    public async Task<DeviceDto?> UpdateDevice(int id, UpdateDeviceRequest r)
        => await PutAsync<DeviceDto, UpdateDeviceRequest>($"{ApiRoutes.Devices.Base}/{id}", r);

    public async Task DeleteDevice(int id)
        => await DeleteAsync($"{ApiRoutes.Devices.Base}/{id}");

    // ── Inventory ───────────────────────────────────────
    public async Task<List<InventoryDto>> GetInventory()
        => await GetAsync<List<InventoryDto>>(ApiRoutes.Inventory.Base) ?? [];

    public async Task<List<InventoryDto>> GetInventoryByStore(int storeId)
        => await GetAsync<List<InventoryDto>>($"{ApiRoutes.Inventory.Base}/store/{storeId}") ?? [];

    public async Task<InventoryDto> UpsertInventory(UpdateInventoryRequest r)
        => (await PutAsync<InventoryDto, UpdateInventoryRequest>(ApiRoutes.Inventory.Base, r))!;

    // ── Sales ───────────────────────────────────────────
    public async Task<List<SaleTransactionDto>> GetSales()
        => await GetAsync<List<SaleTransactionDto>>(ApiRoutes.Sales.Base) ?? [];

    public async Task<List<SaleTransactionDto>> GetSalesByStore(int storeId)
        => await GetAsync<List<SaleTransactionDto>>($"{ApiRoutes.Sales.Base}/store/{storeId}") ?? [];

    // ── Dashboard ───────────────────────────────────────
    public async Task<DashboardSummaryDto> GetDashboard()
        => (await GetAsync<DashboardSummaryDto>(ApiRoutes.Dashboard.Base))!;

    // ── Users ────────────────────────────────────────────
    public async Task<List<UserDto>> GetUsers()
        => await GetAsync<List<UserDto>>(ApiRoutes.Users.Base) ?? [];

    public async Task<UserDto?> GetUser(int id)
        => await GetAsync<UserDto>($"{ApiRoutes.Users.Base}/{id}");

    public async Task<UserDto> CreateUser(CreateUserRequest r)
        => (await PostAsync<UserDto, CreateUserRequest>(ApiRoutes.Users.Base, r))!;

    public async Task<UserDto?> UpdateUser(int id, UpdateUserRequest r)
        => await PutAsync<UserDto, UpdateUserRequest>($"{ApiRoutes.Users.Base}/{id}", r);

    public async Task DeleteUser(int id)
        => await DeleteAsync($"{ApiRoutes.Users.Base}/{id}");

    // ── Roles ────────────────────────────────────────────
    public async Task<List<RoleDto>> GetRoles()
        => await GetAsync<List<RoleDto>>(ApiRoutes.Roles.Base) ?? [];

    public async Task<RoleDto?> GetRole(int id)
        => await GetAsync<RoleDto>($"{ApiRoutes.Roles.Base}/{id}");

    public async Task<RoleDto> CreateRole(CreateRoleRequest r)
        => (await PostAsync<RoleDto, CreateRoleRequest>(ApiRoutes.Roles.Base, r))!;

    public async Task<RoleDto?> UpdateRole(int id, UpdateRoleRequest r)
        => await PutAsync<RoleDto, UpdateRoleRequest>($"{ApiRoutes.Roles.Base}/{id}", r);

    public async Task DeleteRole(int id)
        => await DeleteAsync($"{ApiRoutes.Roles.Base}/{id}");

    // ── Permissions ─────────────────────────────────────
    public async Task<List<PermissionDto>> GetPermissions()
        => await GetAsync<List<PermissionDto>>(ApiRoutes.Permissions.Base) ?? [];

    // ── Multipart Upload ────────────────────────────────
    private async Task<T?> UploadAsync<T>(string url, Stream imageStream, string fileName) where T : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await EnsureAuthenticatedAsync();
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(imageStream), "file", fileName);
            var resp = await _http.PostAsync(FullUri(url), content);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await OnUnauthorizedAsync();
                throw new UnauthorizedAccessException("Session expired. Please log in again.");
            }
            resp.EnsureSuccessStatusCode();
            var apiResp = await resp.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOpts);
            return apiResp?.Data;
        });
    }
}
