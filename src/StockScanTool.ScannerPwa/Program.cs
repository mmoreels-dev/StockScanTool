using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using StockScanTool.ScannerPwa;
using StockScanTool.ScannerPwa.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddScoped<HttpClient>();
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<StockScanTool.Shared.Services.BarcodeScannerService>();

await builder.Build().RunAsync();
