using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StockScanTool.Domain.Entities;
using StockScanTool.Infrastructure.Data;
using Xunit;

namespace StockScanTool.Tests.Unit.Data;

public class AppDbContextTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SaveChanges_ModifiedInventory_BumpsRowVersion()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var inventory = new Inventory
        {
            ProductId = 1,
            StoreId = 1,
            QuantityOnHand = 5,
            RowVersion = 0
        };

        context.Inventories.Add(inventory);
        await context.SaveChangesAsync();
        inventory.RowVersion.Should().Be(0);

        inventory.QuantityOnHand = 10;
        await context.SaveChangesAsync();
        inventory.RowVersion.Should().Be(1);

        inventory.QuantityOnHand = 20;
        await context.SaveChangesAsync();
        inventory.RowVersion.Should().Be(2);
    }

    [Fact]
    public async Task SaveChanges_AddedInventory_DoesNotBumpRowVersion()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var inventory = new Inventory { ProductId = 1, StoreId = 1, QuantityOnHand = 3 };

        context.Inventories.Add(inventory);
        await context.SaveChangesAsync();

        inventory.RowVersion.Should().Be(0);
    }

    [Fact]
    public async Task SaveChanges_UnmodifiedInventory_DoesNotBumpRowVersion()
    {
        await using var context = CreateContext(Guid.NewGuid().ToString());
        var inventory = new Inventory { ProductId = 1, StoreId = 1, QuantityOnHand = 3, RowVersion = 7 };
        context.Inventories.Add(inventory);
        await context.SaveChangesAsync();

        context.Entry(inventory).State = EntityState.Unchanged;
        await context.SaveChangesAsync();

        inventory.RowVersion.Should().Be(7);
    }
}
