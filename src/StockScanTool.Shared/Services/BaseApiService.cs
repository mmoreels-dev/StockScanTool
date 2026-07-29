using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using StockScanTool.Contracts;

namespace StockScanTool.Shared.Services;

public abstract class BaseApiService
{
    protected readonly HttpClient _http;
    protected string _token = string.Empty;
    private string _baseUrl = "";

    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(1000)
    ];

    protected static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected BaseApiService(HttpClient http) => _http = http;

    public void SetBaseUrl(string url)
    {
        _baseUrl = url.TrimEnd('/') + "/";
    }

    public string GetBaseUrl() => _baseUrl;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    protected void AttachToken() =>
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);

    protected void ClearToken() =>
        _http.DefaultRequestHeaders.Authorization = null;

    protected Uri FullUri(string url) =>
        new(new Uri(_baseUrl, UriKind.Absolute), url);

    protected virtual Task EnsureAuthenticatedAsync() => Task.CompletedTask;

    public Action? SessionExpired { get; set; }

    protected virtual Task OnUnauthorizedAsync()
    {
        SessionExpired?.Invoke();
        return Task.CompletedTask;
    }

    protected async Task<T?> GetAsync<T>(string url) where T : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await EnsureAuthenticatedAsync();
            var response = await _http.GetAsync(FullUri(url));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await OnUnauthorizedAsync();
                throw new UnauthorizedAccessException("Session expired. Please log in again.");
            }
            if (!response.IsSuccessStatusCode) return null;
            var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOpts);
            return apiResp?.Data;
        });
    }

    protected async Task<TOut?> PostAsync<TOut, TIn>(string url, TIn body) where TOut : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await EnsureAuthenticatedAsync();
            var response = await _http.PostAsJsonAsync(FullUri(url), body, JsonOpts);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await OnUnauthorizedAsync();
                throw new UnauthorizedAccessException("Session expired. Please log in again.");
            }
            if (!response.IsSuccessStatusCode) return null;
            var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<TOut>>(JsonOpts);
            return apiResp?.Data;
        });
    }

    protected async Task<TOut?> PutAsync<TOut, TIn>(string url, TIn body) where TOut : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await EnsureAuthenticatedAsync();
            var response = await _http.PutAsJsonAsync(FullUri(url), body, JsonOpts);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await OnUnauthorizedAsync();
                throw new UnauthorizedAccessException("Session expired. Please log in again.");
            }
            if (!response.IsSuccessStatusCode) return null;
            var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<TOut>>(JsonOpts);
            return apiResp?.Data;
        });
    }

    protected async Task<bool> DeleteAsync(string url)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await EnsureAuthenticatedAsync();
            var response = await _http.DeleteAsync(FullUri(url));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await OnUnauthorizedAsync();
                throw new UnauthorizedAccessException("Session expired. Please log in again.");
            }
            return response.IsSuccessStatusCode;
        });
    }

    protected static async Task<T?> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && attempt < MaxRetries)
            {
                await Task.Delay(RetryDelays[attempt]);
            }
        }
    }
}
