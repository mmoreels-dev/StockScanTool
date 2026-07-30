using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using StockScanTool.Api.Extensions;
using StockScanTool.Api.Middleware;
using StockScanTool.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Kestrel ---
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5168);

    var certPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "certs", "cert.pfx"));
    if (!File.Exists(certPath))
    {
        return;
    }

    var certPassword = builder.Configuration["HttpsCertPassword"];
    try
    {
        var certificate = string.IsNullOrWhiteSpace(certPassword)
            ? new X509Certificate2(certPath)
            : new X509Certificate2(certPath, certPassword);

        options.ListenAnyIP(5169, listenOptions =>
        {
            listenOptions.UseHttps(certificate);
        });
    }
    catch (CryptographicException ex)
    {
        Console.WriteLine($"Warning: Unable to load HTTPS certificate from {certPath}: {ex.Message}");
    }
});

// --- Database ---
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite", true);
var connectionString = useSqlite
    ? builder.Configuration.GetConnectionString("SqliteConnection")
        ?? throw new InvalidOperationException("SqliteConnection is not configured.")
    : builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");

var apiKeyPepper = builder.Configuration["ApiKeyHashing:Pepper"]
    ?? throw new InvalidOperationException("ApiKeyHashing:Pepper is not configured.");
builder.Services.AddInfrastructure(connectionString, useSqlite, apiKeyPepper);

// --- JWT + Auth ---
builder.Services.AddJwtAuthentication(builder.Configuration);

// --- Rate Limiting ---
builder.Services.AddRateLimiting();

// --- Controllers + OpenAPI ---
builder.Services.AddControllers();
builder.Services.AddOpenApiWithJwt();

// --- CORS ---
builder.Services.AddCorsFromConfig(builder.Configuration, builder.Environment);

var app = builder.Build();

// --- Seed Database ---
await app.SeedDatabaseAsync();

// --- Static Files ---
app.EnsureUploadsDirectory();
app.UseStaticFiles();

// --- Pipeline ---
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
    app.UseSwaggerWithUi();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
