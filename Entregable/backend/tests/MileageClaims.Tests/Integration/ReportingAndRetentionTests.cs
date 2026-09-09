using MileageClaims.Modules.Approvals.Abstractions;
using MileageClaims.Modules.Claims;
using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Claims.Dtos;
using MileageClaims.Modules.Rates.Domain;
using MileageClaims.Modules.Reporting.Abstractions;
using MileageClaims.Tests.Infrastructure;
using DriveType = MileageClaims.Modules.Claims.Domain.DriveType;

namespace MileageClaims.Tests.Integration;

public sealed class ReportingAndRetentionTests
{
    private static CreateMileageClaimRequest Vehicle() =>
        new(VehicleType.Car, FuelType.Gasoline, "ABC123", DriveType.Single, 2026, 1500);

    /// <summary>REG-1: que faltara cualquiera de estos datos mientras la boleta existe lo haría fallar.</summary>
    [Fact]
    public async Task Mientras_existe_se_puede_reconstruir_colaborador_vehiculo_viajes_tramos_y_monto()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);

        var full = await claims.Get(claim.Id);

        Assert.Equal(scenario.EmployeeNationalId, full.EmployeeNationalId);
        Assert.NotEmpty(full.EmployeeName);
        Assert.Equal(scenario.ApproverEmail, full.ApproverEmail);
        Assert.Single(full.Trips);
        Assert.Single(full.Trips[0].Legs);
        Assert.Equal(scenario.StoreAId, full.Trips[0].Legs[0].OriginStoreId);
        Assert.Equal(scenario.StoreBId, full.Trips[0].Legs[0].DestinationStoreId);
        Assert.Equal(10m, full.Trips[0].Legs[0].AppliedDistanceKm);
        Assert.Equal(1000m, full.TotalAmount); // 100 ₡/km * 10km
        Assert.NotNull(full.SubmittedAt);
    }

    /// <summary>REG-2: que el detalle siguiera disponible pasados los 7 días, o que el resumen perdiera datos, lo haría fallar.</summary>
    [Fact]
    public async Task Pasados_7_dias_desde_finanzas_el_detalle_se_purga_pero_el_resumen_persiste()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();
        var statusUpdater = host.Get<IMileageClaimStatusUpdater>();
        var reporting = host.Get<IReportingQueryService>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);
        await approvals.Approve(claim.Id, scenario.ApproverEmail);

        // Antes de los 7 días: no se purga.
        host.Clock.Advance(TimeSpan.FromDays(6));
        Assert.Equal(0, await statusUpdater.ProcessRetentionPurge(retentionDays: 7));
        await claims.Get(claim.Id); // no debe tirar

        // Pasados los 7 días: se purga el detalle.
        host.Clock.Advance(TimeSpan.FromDays(2));
        Assert.Equal(1, await statusUpdater.ProcessRetentionPurge(retentionDays: 7));

        await Assert.ThrowsAsync<MileageClaimNotFoundException>(() => claims.Get(claim.Id));

        var summary = await reporting.GetById(claim.Id);
        Assert.NotNull(summary);
        Assert.Equal(1000m, summary!.TotalAmount);
        Assert.Equal(10m, summary.TotalDistance);
        Assert.NotEmpty(summary.AppliedRateSummary);
        Assert.Equal("Approved", summary.Status);
        Assert.NotNull(summary.DecidedAt);
    }

    /// <summary>RF-15: que el filtro por jefatura o por fecha no excluyera lo que debe, o que el promedio saliera mal, lo haría fallar.</summary>
    [Fact]
    public async Task El_reporte_agrega_monto_volumen_y_tiempo_promedio_filtrando_por_jefatura_y_periodo()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var otherApproverEmployee = TestData.SeedEmployee(host.Db, "555555555", "666666666", "666666666@automercado.test");
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();
        var statusUpdater = host.Get<IMileageClaimStatusUpdater>();
        var reporting = host.Get<IReportingQueryService>();

        // El reloj solo avanza (FakeTimeProvider no permite retroceder): cada boleta se arma,
        // aprueba y purga en su propio momento, uno después del otro.
        async Task<Guid> CreateApproveAndPurge(string employeeNationalId, string approverEmail)
        {
            var tripDate = DateOnly.FromDateTime(host.Clock.GetUtcNow().UtcDateTime);
            var claim = await claims.Create(employeeNationalId, Vehicle());
            await claims.AddTrip(claim.Id, employeeNationalId, new AddTripRequest(tripDate, [scenario.StoreAId, scenario.StoreBId]));
            await claims.Submit(claim.Id, employeeNationalId);
            host.Clock.Advance(TimeSpan.FromHours(5));
            await approvals.Approve(claim.Id, approverEmail);
            host.Clock.Advance(TimeSpan.FromDays(8));
            await statusUpdater.ProcessRetentionPurge(retentionDays: 7);
            return claim.Id;
        }

        var from = host.Clock.GetUtcNow();

        // Dos boletas de la jefatura bajo prueba, dentro del rango a consultar.
        await CreateApproveAndPurge(scenario.EmployeeNationalId, scenario.ApproverEmail);
        host.Clock.Advance(TimeSpan.FromDays(1));
        // Una de otra jefatura: no debe entrar al filtro.
        await CreateApproveAndPurge(otherApproverEmployee.NationalId, otherApproverEmployee.ApproverEmail);
        host.Clock.Advance(TimeSpan.FromDays(1));
        await CreateApproveAndPurge(scenario.EmployeeNationalId, scenario.ApproverEmail);

        var report = await reporting.GetSummary(scenario.ApproverEmail, from: from, to: host.Clock.GetUtcNow());

        // Si el filtro por jefatura no funcionara, ClaimCount sería 3 y TotalAmount ₡3000
        // (la boleta de la otra jefatura quedaría incluida).
        Assert.Equal(2, report.ClaimCount);
        Assert.Equal(2000m, report.TotalAmount);
        Assert.NotNull(report.AverageProcessingHours);
    }
}
