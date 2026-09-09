using MileageClaims.Modules.Rates.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MileageClaims.Modules.Rates.Persistence;

public sealed class RateTableEntryConfiguration : IEntityTypeConfiguration<RateTableEntry>
{
    public void Configure(EntityTypeBuilder<RateTableEntry> builder)
    {
        builder.ToTable("RateTable");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.RatePerKm).HasColumnType("decimal(18,4)");
        builder.HasIndex(x => new { x.VehicleType, x.FuelType, x.EngineDisplacementMin, x.EngineDisplacementMax, x.VehicleAgeYears });
    }
}
