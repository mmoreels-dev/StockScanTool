using Microsoft.JSInterop;

namespace StockScanTool.Web.Services;

public class AuthStateService
{
    private readonly IJSRuntime _js;
    private bool _initialized;
    private string? _token;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
    public string? Token => _token;

    public AuthStateService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _token = await _js.InvokeAsync<string?>("authGetToken");
        }
        catch
        {
            _token = null;
        }

        _initialized = true;
    }

    public void SetToken(string token)
    {
        _token = token;
    }

    public async Task LogoutAsync()
    {
        _token = null;
        try
        {
            await _js.InvokeVoidAsync("authLogout");
        }
        catch { }
    }
}
