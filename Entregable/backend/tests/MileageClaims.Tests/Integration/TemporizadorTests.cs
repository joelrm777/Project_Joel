using MileageClaims.Modules.Approvals.Abstractions;
using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Claims.Dtos;
using MileageClaims.Modules.Notifications.Domain;
using MileageClaims.Modules.Rates.Domain;
using MileageClaims.Tests.Infrastructure;
using DriveType = MileageClaims.Modules.Claims.Domain.DriveType;
using MileageClaimStatus = MileageClaims.Modules.Claims.Domain.MileageClaimStatus;

namespace MileageClaims.Tests.Integration;

public sealed class TemporizadorTests
{
    private static CreateMileageClaimRequest Vehicle() =>
        new(VehicleType.Car, FuelType.Gasoline, "ABC123", DriveType.Single, 2026, 1500);

    /// <summary>Días hábiles según el enunciado: lunes a viernes. Cálculo independiente del de producción.</summary>
    private static DateTimeOffset AddBusinessDays(DateTimeOffset from, int businessDays)
    {
        var result = from;
        var added = 0;
        while (added < businessDays)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) added++;
        }
        return result;
    }

    private static DateTimeOffset NextWeekday(DateTimeOffset from)
    {
        var d = from;
        while (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) d = d.AddDays(1);
        return d;
    }

    /// <summary>RN-14: que se mandara antes de tiempo, o que no se repitiera, lo haría fallar.</summary>
    [Fact]
    public async Task Sin_decision_en_el_plazo_manda_recordatorio_y_se_repite_si_sigue_pendiente()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();

        // Posterior al reloj inicial de TestHost (2026-06-15) — FakeTimeProvider no permite retroceder.
        var start = NextWeekday(new DateTimeOffset(2026, 6, 16, 9, 0, 0, TimeSpan.Zero));
        host.Clock.SetUtcNow(start);

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 5, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);

        // A 1 día hábil: todavía no toca (el plazo por defecto es 2).
        host.Clock.SetUtcNow(AddBusinessDays(start, 1));
        Assert.Equal(0, await approvals.ProcessOverdueReminders());

        // A 2 días hábiles: dispara el primer recordatorio.
        host.Clock.SetUtcNow(AddBusinessDays(start, 2));
        Assert.Equal(1, await approvals.ProcessOverdueReminders());
        var firstReminderAt = host.Clock.GetUtcNow();

        // Inmediatamente después no repite (no pasó el plazo desde el último recordatorio).
        Assert.Equal(0, await approvals.ProcessOverdueReminders());

        // A 2 días hábiles del último recordatorio: se repite.
        host.Clock.SetUtcNow(AddBusinessDays(firstReminderAt, 2));
        Assert.Equal(1, await approvals.ProcessOverdueReminders());

        var reminders = host.Db.Set<NotificationLog>().Count(n => n.Type == NotificationType.ApprovalReminder);
        Assert.Equal(2, reminders);
    }

    /// <summary>Que un fin de semana contara como día hábil (o se contara al revés) lo haría fallar.</summary>
    [Fact]
    public async Task Un_fin_de_semana_no_cuenta_como_dia_habil_para_el_recordatorio()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();

        // Buscar un viernes posterior al reloj inicial de TestHost (2026-06-15).
        var friday = new DateTimeOffset(2026, 6, 16, 9, 0, 0, TimeSpan.Zero);
        while (friday.DayOfWeek != DayOfWeek.Friday) friday = friday.AddDays(1);
        host.Clock.SetUtcNow(friday);

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 5, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);

        // Lunes siguiente: 1 día hábil (el sábado y el domingo no cuentan) — todavía no toca.
        host.Clock.SetUtcNow(friday.AddDays(3));
        Assert.Equal(0, await approvals.ProcessOverdueReminders());

        // Martes: 2 días hábiles — ahora sí.
        host.Clock.SetUtcNow(friday.AddDays(4));
        Assert.Equal(1, await approvals.ProcessOverdueReminders());
    }

    /// <summary>RN-13: que se descartara antes de los 7 días, o que nunca se descartara, lo haría fallar.</summary>
    [Fact]
    public async Task Rechazada_sin_corregir_en_7_dias_se_descarta_automaticamente()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();
        var statusUpdater = host.Get<IMileageClaimStatusUpdater>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 5, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);
        var rejectedAt = host.Clock.GetUtcNow();
        await approvals.Reject(claim.Id, scenario.ApproverEmail, "corregir placa");

        // A menos de 7 días: no se toca.
        host.Clock.Advance(TimeSpan.FromDays(6));
        Assert.Equal(0, await statusUpdater.ProcessOverdueDiscards(retentionDays: 7));
        Assert.Equal(MileageClaimStatus.Rejected, (await claims.Get(claim.Id)).Status);

        // A más de 7 días desde el rechazo: se descarta.
        host.Clock.Advance(TimeSpan.FromDays(2));
        Assert.Equal(1, await statusUpdater.ProcessOverdueDiscards(retentionDays: 7));
        Assert.Equal(MileageClaimStatus.Discarded, (await claims.Get(claim.Id)).Status);
    }
}
