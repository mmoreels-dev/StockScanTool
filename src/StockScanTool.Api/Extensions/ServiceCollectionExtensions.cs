using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StockScanTool.Api.Authorization;
using StockScanTool.Infrastructure.Services;
using System.Text;
using System.Threading.RateLimiting;

namespace StockScanTool.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT SecretKey must be at least 32 characters long. " +
        "Set the JWT_SECRET_KEY environment variable or JwtSettings:SecretKey in configuration.");
}

        configuration["JwtSettings:SecretKey"] = jwtKey;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
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

        return services;
    }

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
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

        return services;
    }

    public static IServiceCollection AddOpenApiWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi(options =>
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

        return services;
    }

    public static IServiceCollection AddCorsFromConfig(
        this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                if (origins is null || origins.Length == 0)
                {
                    if (environment.IsDevelopment())
                    {
                        origins = ["https://localhost:5001", "http://localhost:5000", "http://localhost:5050"];
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "CORS:AllowedOrigins must be configured. " +
                            "Add a Cors:AllowedOrigins section to your configuration.");
                    }
                }
                policy.WithOrigins(origins)
                      .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                      .WithHeaders("Authorization", "Content-Type", "Accept");
            });
        });

        return services;
    }
}