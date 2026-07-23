using StockScanTool.Web.Components;
using StockScanTool.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    var certPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "certs", "cert.pfx");
    if (File.Exists(certPath))
    {
        options.ListenAnyIP(5000);
        options.ListenAnyIP(5443, listenOptions =>
        {
            listenOptions.UseHttps(certPath, "stock123");
        });
    }
    else
    {
        options.ListenAnyIP(5000);
    }
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5168");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
