using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Application.Services;
using StockScanTool.Infrastructure.Data;

namespace StockScanTool.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        try
        {
            if (context.Database.IsRelational())
                await context.Database.MigrateAsync();
            else
                await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or SqliteException)
        {
            app.Logger.LogWarning(ex,
                "Database migration failed. Falling back to EnsureCreated without deleting existing data.");
            await context.Database.EnsureCreatedAsync();
        }
        var adminPassword = app.Configuration["Admin:DefaultPassword"] ?? "admin";
        await SeedData.InitializeAsync(context, passwordHasher, adminPassword);
    }

    public static void EnsureUploadsDirectory(this WebApplication app)
    {
        var webRoot = app.Environment.WebRootPath;
        if (webRoot is not null && !Directory.Exists(Path.Combine(webRoot, "uploads", "products")))
            Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "products"));
    }

    public static void UseSwaggerWithUi(this WebApplication app)
    {
        app.MapOpenApi();
    }
}