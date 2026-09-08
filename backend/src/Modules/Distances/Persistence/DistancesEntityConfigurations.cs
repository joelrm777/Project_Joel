using MileageClaims.Modules.Distances.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MileageClaims.Modules.Distances.Persistence;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Store");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
    }
}

public sealed class StoreDistanceConfiguration : IEntityTypeConfiguration<StoreDistance>
{
    public void Configure(EntityTypeBuilder<StoreDistance> builder)
    {
        builder.ToTable("StoreDistance");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DistanceKm).HasColumnType("decimal(10,3)");
        builder.HasIndex(x => new { x.OriginStoreId, x.DestinationStoreId }).IsUnique();
    }
}
