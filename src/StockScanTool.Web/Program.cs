using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using StockScanTool.Web.Components;
using StockScanTool.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? builder.HostEnvironment.BaseAddress
    ?? "http://localhost:5168";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
