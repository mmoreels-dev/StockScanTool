using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StockScanTool.Api.Authorization;
using StockScanTool.Api.Middleware;
using StockScanTool.Infrastructure;
using StockScanTool.Infrastructure.Data;
using StockScanTool.Infrastructure.Services;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    var certPath = Path.Combine(builder.Environment.ContentRootPath, "..", "..", "certs", "cert.pfx");
    if (File.Exists(certPath))
    {
        options.ListenAnyIP(5168);
        options.ListenAnyIP(5169, listenOptions =>
        {
            listenOptions.UseHttps(certPath, builder.Configuration["HttpsCertPassword"] ?? "stock123");
        });
    }
    else
    {
        options.ListenAnyIP(5168);
    }
});

// --- Database ---
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite", true);
var connectionString = useSqlite
    ? builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=StockScanTool.db"
    : builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("No connection string configured.");

builder.Services.AddInfrastructure(connectionString, useSqlite);

// --- JWT Settings ---
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        builder.Configuration["JwtSettings:SecretKey"] = jwtKey;
    }
    else
    {
        throw new InvalidOperationException(
            "JWT SecretKey must be at least 32 characters long. " +
            "Set the JWT_SECRET_KEY environment variable or JwtSettings:SecretKey in configuration.");
    }
}

builder.Configuration["JwtSettings:SecretKey"] = jwtKey;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    var permissions = new[]
    {
        "dashboard.read",
        "stores.read", "stores.create", "stores.update", "stores.delete",
        "products.read", "products.create", "products.update", "products.delete",
        "devices.read", "devices.create", "devices.update", "devices.delete",
        "inventory.read", "inventory.create", "inventory.update",
        "sales.read", "sales.create",
        "users.read", "users.create", "users.update", "users.delete",
        "roles.read", "roles.create", "roles.update", "roles.delete",
        "scanning.sell"
    };

    foreach (var permission in permissions)
    {
        options.AddPolicy($"{HasPermissionAttribute.PolicyPrefix}{permission}",
            policy => policy.Requirements.Add(new PermissionRequirement(permission)));
    }
});

// --- Rate Limiting ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// --- Controllers + OpenAPI ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo { Title = "StockScanTool API", Version = "v1" };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token"
        };
        return Task.CompletedTask;
    });
    options.AddOperationTransformer((operation, context, ct) =>
    {
        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer"),
                new List<string>()
            }
        });
        return Task.CompletedTask;
    });
});

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["https://localhost:5443", "http://localhost:5000", "http://localhost:5050"];
        policy.WithOrigins(origins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .WithHeaders("Authorization", "Content-Type", "Accept");
    });
});

var app = builder.Build();

// --- Seed Database ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();
    }
    catch (Exception ex) when (ex is InvalidOperationException or SqliteException)
    {
        if (app.Environment.IsDevelopment())
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }
    }
    await SeedData.InitializeAsync(context);
}

// --- Static Files ---
var webRoot = app.Environment.WebRootPath;
if (webRoot is not null && !Directory.Exists(Path.Combine(webRoot, "uploads", "products")))
    Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "products"));

app.UseStaticFiles();

// --- Pipeline ---
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "StockScanTool API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
