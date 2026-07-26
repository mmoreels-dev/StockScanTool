using StockScanTool.Web.Components;
using StockScanTool.Web.Services;
using StockScanTool.Contracts;
using System.Text.Json;

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

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5168");
});
builder.Services.AddScoped<ApiClient>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var http = factory.CreateClient("ApiClient");
    return new ApiClient(http);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapPost("/auth/login", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var request = JsonSerializer.Deserialize<AdminLoginRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (request is null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { success = false, error = "Missing credentials." });
        return;
    }

    var httpClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient");
    var apiResp = await httpClient.PostAsJsonAsync("/api/v1/Auth/admin-login", request);
    var apiBody = await apiResp.Content.ReadAsStringAsync();

    if (!apiResp.IsSuccessStatusCode)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { success = false, error = "Invalid credentials." });
        return;
    }

    var result = JsonSerializer.Deserialize<ApiResponse<AdminLoginResponse>>(apiBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    var token = result?.Data?.Token;

    if (string.IsNullOrEmpty(token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { success = false, error = "Login failed." });
        return;
    }

    context.Response.Cookies.Append("auth_token", token, new CookieOptions
    {
        HttpOnly = false,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = TimeSpan.FromHours(24)
    });

    context.Response.StatusCode = 200;
    await context.Response.WriteAsJsonAsync(new { success = true, token });
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
