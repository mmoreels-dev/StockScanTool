using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Configurations;

public class SaleTransactionConfiguration : IEntityTypeConfiguration<SaleTransaction>
{
    public void Configure(EntityTypeBuilder<SaleTransaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TotalAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(t => t.Store)
            .WithMany(s => s.SaleTransactions)
            .HasForeignKey(t => t.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ScanningDevice)
            .WithMany(d => d.SaleTransactions)
            .HasForeignKey(t => t.ScanningDeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
