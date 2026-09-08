using MileageClaims.Modules.Reporting.Domain;

namespace MileageClaims.Modules.Reporting.Abstractions;

/// <summary>Lo que Boletas usa para entregar el resumen antes de purgar el detalle (REG-2).</summary>
public interface IMileageClaimSummaryStore
{
    Task Save(MileageClaimSummary summary, CancellationToken ct = default);
}

public sealed record SummaryReportRow(
    Guid MileageClaimId,
    string EmployeeName,
    decimal TotalAmount,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? DecidedAt,
    string Status);

public sealed record SummaryReport(
    decimal TotalAmount,
    int ClaimCount,
    double? AverageProcessingHours,
    IReadOnlyList<SummaryReportRow> Rows);

/// <summary>Lo que consume el rol Finanzas/Administrador para RF-15.</summary>
public interface IReportingQueryService
{
    Task<SummaryReport> GetSummary(string? approverEmail, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default);
    Task<MileageClaimSummary?> GetById(Guid claimId, CancellationToken ct = default);
}
