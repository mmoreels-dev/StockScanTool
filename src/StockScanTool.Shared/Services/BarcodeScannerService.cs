using Microsoft.JSInterop;

namespace StockScanTool.Shared.Services;

public class BarcodeScannerService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<BarcodeScannerService>? _dotNetRef;

    public event Func<string, Task>? OnBarcodeDetected;
    public bool IsScanning { get; private set; }

    public BarcodeScannerService(IJSRuntime js) => _js = js;

    public async Task StartScanningAsync(string videoElementId)
    {
        if (IsScanning) return;

        _dotNetRef = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("stockScan.startContinuous", videoElementId, _dotNetRef);
        IsScanning = true;
    }

    public async Task StopScanningAsync()
    {
        if (!IsScanning) return;

        await _js.InvokeVoidAsync("stockScan.stop");
        IsScanning = false;
    }

    [JSInvokable]
    public async Task OnBarcodeFound(string barcode)
    {
        if (OnBarcodeDetected is not null)
            await OnBarcodeDetected.Invoke(barcode);
    }

    public async ValueTask DisposeAsync()
    {
        if (IsScanning)
            await StopScanningAsync();
        _dotNetRef?.Dispose();
    }
}
