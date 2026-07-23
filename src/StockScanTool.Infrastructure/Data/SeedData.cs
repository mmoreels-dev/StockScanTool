using Microsoft.EntityFrameworkCore;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        if (await context.Stores.AnyAsync())
            return;

        context.Stores.AddRange(
            new Store { Name = "Downtown Branch", Address = "123 Main St, City Center", IsActive = true },
            new Store { Name = "Mall Location", Address = "456 Commerce Ave, Shopping Mall", IsActive = true },
            new Store { Name = "Airport Terminal", Address = "789 Airport Rd, Terminal 2", IsActive = true },
            new Store { Name = "Suburban Plaza", Address = "321 Oak Lane, Suburbia", IsActive = true },
            new Store { Name = "Industrial Park", Address = "654 Factory Blvd, Industrial Zone", IsActive = true },
            new Store { Name = "Waterfront Store", Address = "987 Harbor Dr, Waterfront", IsActive = true }
        );

        await context.SaveChangesAsync();
    }
}
