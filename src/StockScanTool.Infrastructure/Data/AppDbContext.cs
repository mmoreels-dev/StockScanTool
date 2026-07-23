using Microsoft.EntityFrameworkCore;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ScanningDevice> ScanningDevices => Set<ScanningDevice>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<SaleTransaction> SaleTransactions => Set<SaleTransaction>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Inventory>()
            .HasIndex(i => new { i.ProductId, i.StoreId })
            .IsUnique();
    }
}
