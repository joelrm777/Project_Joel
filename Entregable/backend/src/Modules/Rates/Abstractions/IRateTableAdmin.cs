using MileageClaims.Modules.Rates.Domain;

namespace MileageClaims.Modules.Rates.Abstractions;

/// <summary>Mantenimiento de la tabla de tarifas por parte del administrador (RF-13).</summary>
public interface IRateTableAdmin
{
    Task<IReadOnlyList<RateTableEntry>> GetAll(CancellationToken ct = default);
    Task<RateTableEntry> Create(RateTableEntry entry, CancellationToken ct = default);
    Task<RateTableEntry> Update(Guid id, RateTableEntry entry, CancellationToken ct = default);
}
