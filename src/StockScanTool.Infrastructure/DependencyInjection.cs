using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockScanTool.Application.Repositories;
using StockScanTool.Application.Services;
using StockScanTool.Infrastructure.Data;
using StockScanTool.Infrastructure.Repositories;
using StockScanTool.Infrastructure.Services;

namespace StockScanTool.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, bool useSqlite = true)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            if (useSqlite)
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString);
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ISaleTransactionRepository, SaleTransactionRepository>();
        services.AddScoped<IScanningDeviceRepository, ScanningDeviceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddValidatorsFromAssemblyContaining<StockScanTool.Contracts.Validators.CreateProductRequestValidator>();

        return services;
    }
}
