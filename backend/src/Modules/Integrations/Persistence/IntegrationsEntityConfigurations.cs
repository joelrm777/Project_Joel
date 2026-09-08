using MileageClaims.Modules.Integrations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MileageClaims.Modules.Integrations.Persistence;

public sealed class FakeEmployeeRecordConfiguration : IEntityTypeConfiguration<FakeEmployeeRecord>
{
    public void Configure(EntityTypeBuilder<FakeEmployeeRecord> builder)
    {
        builder.ToTable("FakeErpEmployee");
        builder.HasKey(x => x.NationalId);
        builder.Property(x => x.NationalId).HasMaxLength(20);
    }
}

public sealed class DirectoryAccountConfiguration : IEntityTypeConfiguration<DirectoryAccount>
{
    public void Configure(EntityTypeBuilder<DirectoryAccount> builder)
    {
        builder.ToTable("FakeDirectoryAccount");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
