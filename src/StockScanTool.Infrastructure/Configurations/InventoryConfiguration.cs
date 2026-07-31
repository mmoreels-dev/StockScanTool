using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.RowVersion)
            .IsConcurrencyToken();

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Inventories)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Store)
            .WithMany(s => s.Inventories)
            .HasForeignKey(i => i.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
