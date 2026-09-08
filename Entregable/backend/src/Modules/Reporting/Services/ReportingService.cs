using MileageClaims.Infrastructure;
using MileageClaims.Modules.Reporting.Abstractions;
using MileageClaims.Modules.Reporting.Domain;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Reporting.Services;

public sealed class ReportingService : IMileageClaimSummaryStore, IReportingQueryService
{
    private readonly AppDbContext _db;

    public ReportingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task Save(MileageClaimSummary summary, CancellationToken ct = default)
    {
        _db.Set<MileageClaimSummary>().Add(summary);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<SummaryReport> GetSummary(string? approverEmail, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var query = _db.Set<MileageClaimSummary>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(approverEmail)) query = query.Where(s => s.ApproverEmail == approverEmail);
        if (from is not null) query = query.Where(s => s.SubmittedAt >= from);
        if (to is not null) query = query.Where(s => s.SubmittedAt <= to);

        var rows = await query.OrderByDescending(s => s.SubmittedAt).ToListAsync(ct);

        var processingHours = rows
            .Where(r => r.DecidedAt is not null)
            .Select(r => (r.DecidedAt!.Value - r.SubmittedAt).TotalHours)
            .ToList();

        return new SummaryReport(
            rows.Sum(r => r.TotalAmount),
            rows.Count,
            processingHours.Count > 0 ? processingHours.Average() : null,
            rows.Select(r => new SummaryReportRow(r.MileageClaimId, r.EmployeeName, r.TotalAmount, r.SubmittedAt, r.DecidedAt, r.Status)).ToList());
    }

    public async Task<MileageClaimSummary?> GetById(Guid claimId, CancellationToken ct = default) =>
        await _db.Set<MileageClaimSummary>().FirstOrDefaultAsync(s => s.MileageClaimId == claimId, ct);
}
