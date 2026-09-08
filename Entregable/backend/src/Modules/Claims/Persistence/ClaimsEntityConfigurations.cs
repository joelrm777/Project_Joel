using MileageClaims.Modules.Claims.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MileageClaims.Modules.Claims.Persistence;

public sealed class MileageClaimConfiguration : IEntityTypeConfiguration<MileageClaim>
{
    public void Configure(EntityTypeBuilder<MileageClaim> builder)
    {
        builder.ToTable("MileageClaim");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeNationalId).IsRequired().HasMaxLength(20);
        builder.Property(x => x.PlateNumber).HasMaxLength(20);
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.HasIndex(x => x.EmployeeNationalId);
        builder.HasIndex(x => new { x.ApproverEmail, x.Status });

        builder.HasMany(x => x.Trips)
            .WithOne(t => t.MileageClaim)
            .HasForeignKey(t => t.MileageClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("Trip");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AppliedRatePerKm).HasColumnType("decimal(18,4)");
        builder.Property(x => x.TotalDistance).HasColumnType("decimal(10,3)");
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");

        builder.HasMany(x => x.Legs)
            .WithOne()
            .HasForeignKey(l => l.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LegConfiguration : IEntityTypeConfiguration<Leg>
{
    public void Configure(EntityTypeBuilder<Leg> builder)
    {
        builder.ToTable("Leg");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AppliedDistanceKm).HasColumnType("decimal(10,3)");
    }
}
