using System.Net.Http.Json;
using StockScanTool.Contracts;

namespace StockScanTool.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    // --- Stores ---
    public Task<List<StoreDto>> GetStores() => GetAsync<List<StoreDto>>("api/stores")!;
    public Task<StoreDto?> GetStore(int id) => GetAsync<StoreDto>($"api/stores/{id}");
    public Task<StoreDto> CreateStore(CreateStoreRequest r) => PostAsync<StoreDto, CreateStoreRequest>("api/stores", r);
    public Task<StoreDto?> UpdateStore(int id, UpdateStoreRequest r) => PutAsync<StoreDto, UpdateStoreRequest>($"api/stores/{id}", r);
    public Task DeleteStore(int id) => DeleteAsync($"api/stores/{id}");

    // --- Products ---
    public Task<List<ProductDto>> GetProducts() => GetAsync<List<ProductDto>>("api/products")!;
    public Task<ProductDto?> GetProduct(int id) => GetAsync<ProductDto>($"api/products/{id}");
    public Task<ProductDto> CreateProduct(CreateProductRequest r) => PostAsync<ProductDto, CreateProductRequest>("api/products", r);
    public Task<ProductDto?> UpdateProduct(int id, UpdateProductRequest r) => PutAsync<ProductDto, UpdateProductRequest>($"api/products/{id}", r);
    public Task DeleteProduct(int id) => DeleteAsync($"api/products/{id}");

    // --- Devices ---
    public Task<List<DeviceDto>> GetDevices() => GetAsync<List<DeviceDto>>("api/devices")!;
    public Task<DeviceDto?> GetDevice(int id) => GetAsync<DeviceDto>($"api/devices/{id}");
    public Task<DeviceDto> CreateDevice(CreateDeviceRequest r) => PostAsync<DeviceDto, CreateDeviceRequest>("api/devices", r);
    public Task<DeviceDto?> UpdateDevice(int id, UpdateDeviceRequest r) => PutAsync<DeviceDto, UpdateDeviceRequest>($"api/devices/{id}", r);
    public Task DeleteDevice(int id) => DeleteAsync($"api/devices/{id}");

    // --- Inventory ---
    public Task<List<InventoryDto>> GetInventory() => GetAsync<List<InventoryDto>>("api/inventory")!;
    public Task<List<InventoryDto>> GetInventoryByStore(int storeId) => GetAsync<List<InventoryDto>>($"api/inventory/store/{storeId}")!;
    public Task<InventoryDto> UpsertInventory(UpdateInventoryRequest r) => PostAsync<InventoryDto, UpdateInventoryRequest>("api/inventory", r);

    // --- Sales ---
    public Task<List<SaleTransactionDto>> GetSales() => GetAsync<List<SaleTransactionDto>>("api/sales")!;
    public Task<List<SaleTransactionDto>> GetSalesByStore(int storeId) => GetAsync<List<SaleTransactionDto>>($"api/sales/store/{storeId}")!;

    // --- Dashboard ---
    public Task<DashboardSummaryDto> GetDashboard() => GetAsync<DashboardSummaryDto>("api/dashboard")!;

    // --- Helpers ---
    private async Task<T?> GetAsync<T>(string url)
    {
        var resp = await _http.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>();
    }

    private async Task<TOut> PostAsync<TOut, TIn>(string url, TIn body)
    {
        var resp = await _http.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TOut>())!;
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
