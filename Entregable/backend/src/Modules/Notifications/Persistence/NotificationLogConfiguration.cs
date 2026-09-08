using MileageClaims.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MileageClaims.Modules.Notifications.Persistence;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecipientEmail).IsRequired().HasMaxLength(320);
    }
}
