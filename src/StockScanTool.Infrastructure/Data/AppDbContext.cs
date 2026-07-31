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
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Inventory>()
            .HasIndex(i => new { i.ProductId, i.StoreId })
            .IsUnique();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTrackedEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTrackedEntities();
        return base.SaveChanges();
    }

    private void UpdateTrackedEntities()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Inventory>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.RowVersion++;
        }
    }
}
