using System.Net.Http.Json;
using System.Net.Http.Headers;
using StockScanTool.Contracts;

namespace StockScanTool.Shared.Services;

public abstract class BaseApiService
{
    protected readonly HttpClient _http;
    protected string _token = string.Empty;

    protected BaseApiService(HttpClient http) => _http = http;

    public void SetBaseUrl(string url) =>
        _http.BaseAddress = new Uri(url.TrimEnd('/') + "/");

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    protected void AttachToken() =>
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);

    protected void ClearToken() =>
        _http.DefaultRequestHeaders.Authorization = null;

    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task<TOut?> PostAsync<TOut, TIn>(string url, TIn body)
    {
        var response = await _http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<TOut>();
    }

    protected async Task<TOut?> PutAsync<TOut, TIn>(string url, TIn body)
    {
        var response = await _http.PutAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<TOut>();
    }

    protected async Task<bool> DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }
}
