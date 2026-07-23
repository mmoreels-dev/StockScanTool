using Microsoft.JSInterop;

namespace StockScanTool.ScannerPwa.Services;

public class BarcodeScannerService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<BarcodeScannerService>? _dotNetRef;

    public event Action<string>? OnBarcodeDetected;
    public bool IsScanning { get; private set; }

    public BarcodeScannerService(IJSRuntime js) => _js = js;

    public async Task StartScanningAsync(string videoElementId)
    {
        if (IsScanning) return;

        _dotNetRef = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("barcodeScanner.start", videoElementId, _dotNetRef);
        IsScanning = true;
    }

    public async Task StopScanningAsync()
    {
        if (!IsScanning) return;

        await _js.InvokeVoidAsync("barcodeScanner.stop");
        IsScanning = false;
    }

    [JSInvokable]
    public void OnBarcodeFound(string barcode)
    {
        OnBarcodeDetected?.Invoke(barcode);
    }

    public async ValueTask DisposeAsync()
    {
        if (IsScanning)
            await StopScanningAsync();
        _dotNetRef?.Dispose();
    }
}
