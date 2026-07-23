using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls;
using StockScanTool.Scanner.Services;
using StockScanTool.Scanner.Views;

namespace StockScanTool.Scanner;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddHttpClient<ApiService>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ScannerPage>();

        return builder.Build();
    }
}
