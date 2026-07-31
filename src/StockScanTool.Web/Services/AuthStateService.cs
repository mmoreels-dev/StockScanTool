using System.Text.Json;
using Microsoft.JSInterop;

namespace StockScanTool.Web.Services;

public class AuthStateService
{
    private const string AccessTokenKey = "sst_access_token";
    private const string RefreshTokenKey = "sst_refresh_token";

    private readonly IJSRuntime _js;

    private string? _token;
    private string? _refreshToken;
    private string? _displayName;
    private List<string> _roles = [];
    private List<string> _permissions = [];

    public AuthStateService(IJSRuntime js) => _js = js;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
    public string? Token => _token;
    public string? RefreshToken => _refreshToken;
    public string DisplayName => _displayName ?? "User";
    public IReadOnlyList<string> Roles => _roles.AsReadOnly();
    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();

    public async Task InitializeAsync()
    {
        _token = await GetStoredAsync(AccessTokenKey);
        _refreshToken = await GetStoredAsync(RefreshTokenKey);
        if (_token is not null)
            DecodeToken(_token);
    }

    public void SetToken(string token) => SetTokenPair(token, _refreshToken);

    public void SetTokenPair(string token, string? refreshToken)
    {
        _token = token;
        _refreshToken = refreshToken;
        DecodeToken(token);
        _ = PersistAsync();
    }

    public async Task LogoutAsync()
    {
        _token = null;
        _refreshToken = null;
        _displayName = null;
        _roles = [];
        _permissions = [];
        await RemoveStoredAsync(AccessTokenKey);
        await RemoveStoredAsync(RefreshTokenKey);
    }

    private async Task PersistAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("setStorageItem", AccessTokenKey, _token);
            await _js.InvokeVoidAsync("setStorageItem", RefreshTokenKey, _refreshToken);
        }
        catch
        {
            // JS interop unavailable — keep the session in-memory only.
        }
    }

    private async Task<string?> GetStoredAsync(string key)
    {
        try
        {
            var value = await _js.InvokeAsync<string>("getStorageItem", key);
            return string.IsNullOrEmpty(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    private async Task RemoveStoredAsync(string key)
    {
        try
        {
            await _js.InvokeVoidAsync("removeStorageItem", key);
        }
        catch
        {
            // ignore
        }
    }

    private void DecodeToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return;

            var payload = parts[1];
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            payload = payload.Replace('-', '+').Replace('_', '/');

            var bytes = Convert.FromBase64String(payload);
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            _displayName = TryGetString(root, "displayName")
                ?? TryGetString(root, "sub")
                ?? "User";

            _roles = GetStringList(root, "role");
            _permissions = GetStringList(root, "permission");
        }
        catch
        {
            _displayName = "User";
            _roles = [];
            _permissions = [];
        }
    }

    private static string? TryGetString(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String)
            return val.GetString();
        return null;
    }

    private static List<string> GetStringList(JsonElement el, string prop)
    {
        if (el.TryGetProperty(prop, out var val))
        {
            if (val.ValueKind == JsonValueKind.Array)
                return val.EnumerateArray().Select(x => x.GetString()).Where(x => x != null).Cast<string>().ToList();
            if (val.ValueKind == JsonValueKind.String)
                return [val.GetString()!];
        }
        return [];
    }
}
