using StockScanTool.Contracts;
using Microsoft.JSInterop;

namespace StockScanTool.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private string? _token;

    public ApiClient(HttpClient http) => _http = http;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public void SetToken(string token)
    {
        _token = token;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public void Logout()
    {
        _token = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

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

    public async Task<List<InventoryDto>> GetInventory()
        => await GetAsync<List<InventoryDto>>(ApiRoutes.Inventory.Base) ?? [];

    public async Task<List<InventoryDto>> GetInventoryByStore(int storeId)
        => await GetAsync<List<InventoryDto>>($"{ApiRoutes.Inventory.Base}/store/{storeId}") ?? [];

    public async Task<InventoryDto> UpsertInventory(UpdateInventoryRequest r)
        => (await PutAsync<InventoryDto, UpdateInventoryRequest>(ApiRoutes.Inventory.Base, r))!;

    public async Task<List<SaleTransactionDto>> GetSales()
        => await GetAsync<List<SaleTransactionDto>>(ApiRoutes.Sales.Base) ?? [];

    public async Task<List<SaleTransactionDto>> GetSalesByStore(int storeId)
        => await GetAsync<List<SaleTransactionDto>>($"{ApiRoutes.Sales.Base}/store/{storeId}") ?? [];

    public async Task<DashboardSummaryDto> GetDashboard()
        => (await GetAsync<DashboardSummaryDto>(ApiRoutes.Dashboard.Base))!;

    private async Task<T?> GetAsync<T>(string url)
    {
        var resp = await _http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>();
    }

    private async Task<TOut?> PostAsync<TOut, TIn>(string url, TIn body)
    {
        var resp = await _http.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<TOut>();
    }

    private async Task<TOut?> PutAsync<TOut, TIn>(string url, TIn body)
    {
        var resp = await _http.PutAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<TOut>();
    }

    private async Task DeleteAsync(string url)
    {
        var resp = await _http.DeleteAsync(url);
        resp.EnsureSuccessStatusCode();
    }
}
