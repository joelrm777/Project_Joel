using MileageClaims.Infrastructure;
using MileageClaims.Modules.Integrations.Abstractions;
using MileageClaims.Modules.Integrations.Domain;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Integrations.Services;

public sealed class FakeErpRH : IErpRH
{
    private readonly AppDbContext _db;

    public FakeErpRH(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EmployeeInfo?> BuscarPorCedula(string nationalId, CancellationToken ct = default)
    {
        var record = await _db.Set<FakeEmployeeRecord>()
            .FirstOrDefaultAsync(e => e.NationalId == nationalId && e.IsActive, ct);

        return record is null
            ? null
            : new EmployeeInfo(record.NationalId, record.Name, record.Email, record.ApproverNationalId,
                record.ApproverEmail, record.Department, record.JobTitle, record.IsActive);
    }
}
