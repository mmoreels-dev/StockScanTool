using StockScanTool.Scanner.Services;

namespace StockScanTool.Scanner.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _api;

    public LoginPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var server = ServerEntry.Text?.Trim().TrimEnd('/');
        var apiKey = ApiKeyEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(apiKey))
        {
            ShowError("Please enter both server address and API key.");
            return;
        }

        if (!Uri.TryCreate(server, UriKind.Absolute, out _))
        {
            ShowError("Invalid server address. Include http:// or https://");
            return;
        }

        LoginButton.IsEnabled = false;
        LoginSpinner.IsVisible = true;
        LoginSpinner.IsRunning = true;
        ErrorLabel.IsVisible = false;

        _api.BaseUrl = server;

        try
        {
            var success = await _api.LoginAsync(apiKey);
            if (success)
            {
                await Navigation.PushAsync(new ScannerPage(_api));
            }
            else
            {
                ShowError("Invalid API key or device is inactive.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Connection failed: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginSpinner.IsRunning = false;
            LoginSpinner.IsVisible = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}
