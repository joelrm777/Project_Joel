using MileageClaims.Infrastructure;
using MileageClaims.Modules.Notifications.Abstractions;
using MileageClaims.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Notifications.Services;

public sealed class NotificationSender : INotificationSender
{
    private readonly AppDbContext _db;

    public NotificationSender(AppDbContext db)
    {
        _db = db;
    }

    public async Task Send(NotificationType type, string recipientEmail, Guid? mileageClaimId, string details, CancellationToken ct = default)
    {
        _db.Set<NotificationLog>().Add(new NotificationLog
        {
            Id = Guid.NewGuid(),
            Type = type,
            RecipientEmail = recipientEmail,
            MileageClaimId = mileageClaimId,
            Details = details,
            SentAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationLog>> GetLog(CancellationToken ct = default) =>
        await _db.Set<NotificationLog>().OrderByDescending(n => n.SentAt).ToListAsync(ct);
}
