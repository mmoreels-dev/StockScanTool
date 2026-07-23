using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockScanTool.Infrastructure.Data;

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

        return services;
    }
}
