using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockScanTool.Domain.Entities;

namespace StockScanTool.Infrastructure.Configurations;

public class ScanningDeviceConfiguration : IEntityTypeConfiguration<ScanningDevice>
{
    public void Configure(EntityTypeBuilder<ScanningDevice> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DeviceName).IsRequired().HasMaxLength(100);
        builder.Property(d => d.ApiKey).IsRequired().HasMaxLength(256);

        builder.HasIndex(d => d.ApiKey).IsUnique();

        builder.HasOne(d => d.Store)
            .WithMany(s => s.ScanningDevices)
            .HasForeignKey(d => d.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
