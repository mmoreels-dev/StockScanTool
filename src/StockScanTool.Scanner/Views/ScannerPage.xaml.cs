using System.Collections.ObjectModel;
using StockScanTool.Contracts;
using StockScanTool.Scanner.Services;
using ZXing.Net.Maui;

namespace StockScanTool.Scanner.Views;

public partial class ScannerPage : ContentPage
{
    private readonly ApiService _api;
    private readonly ObservableCollection<CartItem> _cart = new();
    private bool _isProcessing;
    private bool _showingFeedback;

    public string StoreName => _api.StoreName;
    public string DeviceName => _api.DeviceName;
    public bool HasItems => _cart.Count > 0;
    public string CartSummary => $"{_cart.Count} item(s) in cart";
    public string CartTotalText => $"Total: {_cart.Sum(c => c.Total):C}";

    public ScannerPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        BindingContext = this;

        BarcodeReader.BarcodesDetected += OnBarcodesDetected;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BarcodeReader.IsDetecting = true;
        _showingFeedback = false;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReader.IsDetecting = false;
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing || _showingFeedback) return;

        var barcode = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrEmpty(barcode)) return;

        _isProcessing = true;
        BarcodeReader.IsDetecting = false;

        try
        {
            var product = await _api.LookupBarcodeAsync(barcode);
            if (product is null)
            {
                await DisplayAlert("Not Found", $"No product found for barcode: {barcode}", "OK");
                return;
            }

            ScannedProductName.Text = product.Name;
            ScannedProductPrice.Text = product.Price.ToString("C");
            ScannedProductMsg.IsVisible = false;
            ScanFeedback.IsVisible = true;
            _showingFeedback = true;

            ScanFeedback.BindingContext = product;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Lookup failed: {ex.Message}", "OK");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private void OnAddToCartClicked(object? sender, EventArgs e)
    {
        if (ScanFeedback.BindingContext is not BarcodeLookupResponse product) return;

        var existing = _cart.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            _cart.Add(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                Barcode = product.Barcode,
                Price = product.Price,
                Quantity = 1
            });
        }

        RefreshCartUi();

        ScanFeedback.IsVisible = false;
        _showingFeedback = false;
        BarcodeReader.IsDetecting = true;
    }

    private async void OnViewCartClicked(object? sender, EventArgs e)
    {
        if (_cart.Count == 0)
        {
            await DisplayAlert("Cart", "Your cart is empty.", "OK");
            return;
        }

        var message = string.Join("\n", _cart.Select(c =>
            $"{c.Name} x{c.Quantity} = {c.Total:C}"));

        await DisplayAlert($"Cart ({_cart.Count} items)",
            $"{message}\n\nTotal: {_cart.Sum(c => c.Total):C}", "OK");
    }

    private async void OnCheckoutClicked(object? sender, EventArgs e)
    {
        if (_cart.Count == 0) return;

        var total = _cart.Sum(c => c.Total);
        var confirm = await DisplayAlert("Confirm Sale",
            $"Complete sale for {_cart.Count} item(s) totalling {total:C}?",
            "Complete Sale", "Cancel");

        if (!confirm) return;

        try
        {
            var result = await _api.SubmitSaleAsync(_cart.ToList());
            if (result is not null)
            {
                await DisplayAlert("Sale Complete",
                    $"Transaction #{result.Id}\nTotal: {result.TotalAmount:C}", "OK");
                _cart.Clear();
                RefreshCartUi();
            }
            else
            {
                await DisplayAlert("Error", "Sale failed. Check stock levels.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Checkout failed: {ex.Message}", "OK");
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Logout", "Disconnect this scanner?", "Yes", "No");
        if (!confirm) return;

        _api.Logout();
        BarcodeReader.IsDetecting = false;
        await Navigation.PopToRootAsync();
    }

    private void RefreshCartUi()
    {
        OnPropertyChanged(nameof(CartSummary));
        OnPropertyChanged(nameof(CartTotalText));
        OnPropertyChanged(nameof(HasItems));
    }
}
