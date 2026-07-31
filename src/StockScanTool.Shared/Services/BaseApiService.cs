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

    public virtual void SetBaseUrl(string url)
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

    protected virtual Task<bool> TryRefreshAsync() => Task.FromResult(false);

    protected async Task<HttpResponseMessage> SendWithRefreshAsync(Func<Task<HttpResponseMessage>> send)
    {
        var response = await send();
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;

        if (await TryRefreshAsync())
        {
            var retried = await send();
            if (retried.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                return retried;
            }
            response.Dispose();
            response = retried;
        }

        await OnUnauthorizedAsync();
        return response;
    }

    protected async Task<T?> GetAsync<T>(string url) where T : class
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            await EnsureAuthenticatedAsync();
            var response = await SendWithRefreshAsync(() => _http.GetAsync(FullUri(url)));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Session expired. Please log in again.");
            await ThrowForErrorAsync(response);
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
            var response = await SendWithRefreshAsync(() => _http.PostAsJsonAsync(FullUri(url), body, JsonOpts));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Session expired. Please log in again.");
            await ThrowForErrorAsync(response);
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
            var response = await SendWithRefreshAsync(() => _http.PutAsJsonAsync(FullUri(url), body, JsonOpts));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Session expired. Please log in again.");
            await ThrowForErrorAsync(response);
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
            var response = await SendWithRefreshAsync(() => _http.DeleteAsync(FullUri(url)));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Session expired. Please log in again.");
            await ThrowForErrorAsync(response);
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

    private static async Task ThrowForErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        // 401 and 404 keep their existing null/false semantics for callers.
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.NotFound)
            return;

        var message = response.StatusCode == System.Net.HttpStatusCode.Forbidden
            ? "You do not have permission to perform this action."
            : $"Request failed with status code {(int)response.StatusCode}.";
        try
        {
            var apiResp = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOpts);
            if (apiResp is not null)
            {
                if (apiResp.Errors is { Count: > 0 } errors)
                    message = string.Join(" ", errors);
                else if (!string.IsNullOrEmpty(apiResp.Error))
                    message = apiResp.Error;
            }
        }
        catch
        {
            // Response body was not the expected shape — keep the status-code message.
        }

        throw new ApiRequestException(message, response.StatusCode);
    }
}

public class ApiRequestException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }

    public ApiRequestException(string message, System.Net.HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
