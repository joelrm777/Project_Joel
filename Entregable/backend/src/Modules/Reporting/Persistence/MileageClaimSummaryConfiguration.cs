using MileageClaims.Modules.Reporting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MileageClaims.Modules.Reporting.Persistence;

public sealed class MileageClaimSummaryConfiguration : IEntityTypeConfiguration<MileageClaimSummary>
{
    public void Configure(EntityTypeBuilder<MileageClaimSummary> builder)
    {
        builder.ToTable("MileageClaimSummary");
        builder.HasKey(x => x.MileageClaimId);
        builder.Property(x => x.MileageClaimId).ValueGeneratedNever();
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalDistance).HasColumnType("decimal(10,3)");
        builder.HasIndex(x => x.ApproverNationalId);
        builder.HasIndex(x => x.SubmittedAt);
    }
}
