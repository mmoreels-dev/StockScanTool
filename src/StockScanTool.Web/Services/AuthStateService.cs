using System.Text.Json;

namespace StockScanTool.Web.Services;

public class AuthStateService
{
    private string? _token;
    private string? _displayName;
    private List<string> _roles = [];
    private List<string> _permissions = [];

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
    public string? Token => _token;
    public string DisplayName => _displayName ?? "User";
    public IReadOnlyList<string> Roles => _roles.AsReadOnly();
    public IReadOnlyList<string> Permissions => _permissions.AsReadOnly();

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public void SetToken(string token)
    {
        _token = token;
        DecodeToken(token);
    }

    public Task LogoutAsync()
    {
        _token = null;
        _displayName = null;
        _roles = [];
        _permissions = [];
        return Task.CompletedTask;
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
