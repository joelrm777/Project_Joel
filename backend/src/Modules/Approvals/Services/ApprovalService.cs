using MileageClaims.Infrastructure;
using MileageClaims.Modules.Approvals.Abstractions;
using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Notifications.Abstractions;
using MileageClaims.Modules.Notifications.Domain;

namespace MileageClaims.Modules.Approvals.Services;

public sealed class ApprovalService : IApprovalService
{
    private const string ReminderIntervalKey = "ReminderIntervalBusinessDays";
    private const int DefaultReminderIntervalBusinessDays = 2;

    private readonly IMileageClaimService _claims;
    private readonly IMileageClaimStatusUpdater _statusUpdater;
    private readonly INotificationSender _notifications;
    private readonly ISystemConfigurationStore _config;
    private readonly TimeProvider _clock;

    public ApprovalService(
        IMileageClaimService claims,
        IMileageClaimStatusUpdater statusUpdater,
        INotificationSender notifications,
        ISystemConfigurationStore config,
        TimeProvider clock)
    {
        _claims = claims;
        _statusUpdater = statusUpdater;
        _notifications = notifications;
        _config = config;
        _clock = clock;
    }

    public async Task Approve(Guid claimId, string approverEmail, CancellationToken ct = default)
    {
        var claim = await _claims.Get(claimId, ct);
        if (!string.Equals(claim.ApproverEmail, approverEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApprovalNotOwnedException(claimId);
        }

        await _statusUpdater.MarkApproved(claimId, ct);

        await _notifications.Send(
            NotificationType.Approved,
            claim.EmployeeEmail,
            claimId,
            $"Tu boleta fue aprobada. Monto: {claim.TotalAmount:C}.",
            ct);
    }

    public async Task Reject(Guid claimId, string approverEmail, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new RejectionReasonRequiredException();
        }

        var claim = await _claims.Get(claimId, ct);
        if (!string.Equals(claim.ApproverEmail, approverEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApprovalNotOwnedException(claimId);
        }

        await _statusUpdater.MarkRejected(claimId, reason, ct);

        await _notifications.Send(
            NotificationType.Rejected,
            claim.EmployeeEmail,
            claimId,
            $"Tu boleta fue rechazada. Motivo: {reason}",
            ct);
    }

    public async Task<int> ProcessOverdueReminders(CancellationToken ct = default)
    {
        var intervalDays = await _config.GetInt(ReminderIntervalKey, DefaultReminderIntervalBusinessDays, ct);
        var now = _clock.GetUtcNow();
        var candidates = await _statusUpdater.FindPendingForReminderCheck(ct);

        var sent = 0;
        foreach (var candidate in candidates)
        {
            var referencePoint = candidate.LastReminderAt ?? candidate.SubmittedAt;
            if (BusinessDayCalculator.BusinessDaysBetween(referencePoint, now) < intervalDays) continue;

            await _notifications.Send(
                NotificationType.ApprovalReminder,
                candidate.ApproverEmail,
                candidate.ClaimId,
                "Tenés una boleta pendiente de aprobación desde hace varios días.",
                ct);

            await _statusUpdater.RecordReminderSent(candidate.ClaimId, ct);
            sent++;
        }

        return sent;
    }
}
