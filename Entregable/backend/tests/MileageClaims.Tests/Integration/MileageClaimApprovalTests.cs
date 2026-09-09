using MileageClaims.Modules.Approvals.Abstractions;
using MileageClaims.Modules.Claims;
using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Claims.Dtos;
using MileageClaims.Modules.Notifications.Domain;
using MileageClaims.Modules.Rates.Abstractions;
using MileageClaims.Modules.Rates.Domain;
using MileageClaims.Tests.Infrastructure;
using DriveType = MileageClaims.Modules.Claims.Domain.DriveType;
using MileageClaimStatus = MileageClaims.Modules.Claims.Domain.MileageClaimStatus;

namespace MileageClaims.Tests.Integration;

public sealed class MileageClaimApprovalTests
{
    private static CreateMileageClaimRequest Vehicle() =>
        new(VehicleType.Car, FuelType.Gasoline, "ABC123", DriveType.Single, 2026, 1500);

    /// <summary>RN-5: que el envío no revisara la distancia, o que se notificara a otro rol, lo haría fallar.</summary>
    [Fact]
    public async Task Falta_un_tramo_bloquea_el_envio_y_avisa_a_administrador_y_finanzas()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var storeD = TestData.SeedStore(host.Db, "D"); // sin distancia registrada hacia ninguna tienda
        var claims = host.Get<IMileageClaimService>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId,
            new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, storeD.Id]));

        await Assert.ThrowsAsync<MissingDistanceException>(
            () => claims.Submit(claim.Id, scenario.EmployeeNationalId));

        var notifications = host.Db.Set<NotificationLog>().Where(n => n.Type == NotificationType.MissingDistance).ToList();
        Assert.Contains(notifications, n => n.RecipientEmail == scenario.AdminEmail);
        Assert.Contains(notifications, n => n.RecipientEmail == scenario.FinanceEmail);

        // Cargada la distancia faltante, la misma boleta se puede enviar sin más cambios.
        TestData.SeedDistance(host.Db, scenario.StoreAId, storeD.Id, 5m);
        var submitted = await claims.Submit(claim.Id, scenario.EmployeeNationalId);
        Assert.Equal(MileageClaimStatus.Pending, submitted.Status);
    }

    /// <summary>RN-8: que se pudiera editar/retirar en Approved, o la boleta de otro colaborador, lo haría fallar.</summary>
    [Fact]
    public async Task Solo_se_edita_o_retira_mientras_esta_pendiente_y_solo_el_dueno()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var otherEmployee = TestData.SeedEmployee(host.Db, "333333333", "222222222", scenario.ApproverEmail);
        var claims = host.Get<IMileageClaimService>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);

        // Mientras Pending: se puede editar.
        var edited = await claims.UpdateVehicle(claim.Id, scenario.EmployeeNationalId,
            new UpdateVehicleRequest(VehicleType.Car, FuelType.Gasoline, "XYZ999", DriveType.Single, 2026, 1500));
        Assert.Equal("XYZ999", edited.PlateNumber);

        // Otro colaborador no puede tocarla.
        await Assert.ThrowsAsync<ForbiddenClaimAccessException>(
            () => claims.UpdateVehicle(claim.Id, otherEmployee.NationalId,
                new UpdateVehicleRequest(VehicleType.Car, FuelType.Gasoline, "HACKED", DriveType.Single, 2026, 1500)));

        var approvals = host.Get<IApprovalService>();
        await approvals.Approve(claim.Id, scenario.ApproverEmail);

        // Ya Approved: ni editar ni retirar.
        await Assert.ThrowsAsync<ClaimNotEditableException>(
            () => claims.UpdateVehicle(claim.Id, scenario.EmployeeNationalId,
                new UpdateVehicleRequest(VehicleType.Car, FuelType.Gasoline, "NOPE", DriveType.Single, 2026, 1500)));
        await Assert.ThrowsAsync<ClaimNotEditableException>(() => claims.Withdraw(claim.Id, scenario.EmployeeNationalId));
    }

    /// <summary>RN-9: que existiera un estado por viaje distinto al de la boleta lo haría fallar.</summary>
    [Fact]
    public async Task Aprobar_una_boleta_multi_viaje_la_aprueba_completa_no_por_viaje()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 2), [scenario.StoreBId, scenario.StoreCId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);

        await approvals.Approve(claim.Id, scenario.ApproverEmail);

        var result = await claims.Get(claim.Id);
        Assert.Equal(MileageClaimStatus.Approved, result.Status);
        Assert.Equal(2, result.Trips.Count);
    }

    /// <summary>RN-10: que una boleta Pending o Rejected apareciera acá lo haría fallar.</summary>
    [Fact]
    public async Task Finanzas_solo_ve_boletas_aprobadas()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();

        var draft = await claims.Create(scenario.EmployeeNationalId, Vehicle()); // nunca se envía

        var pending = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(pending.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(pending.Id, scenario.EmployeeNationalId);

        var approved = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(approved.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 2), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(approved.Id, scenario.EmployeeNationalId);
        await approvals.Approve(approved.Id, scenario.ApproverEmail);

        var financeView = await claims.GetApproved();

        Assert.Single(financeView);
        Assert.Equal(approved.Id, financeView[0].Id);
    }

    /// <summary>RN-11: que se pudiera rechazar sin motivo, o que el motivo no llegara al colaborador, lo haría fallar.</summary>
    [Fact]
    public async Task Rechazar_exige_motivo_y_lo_comunica_al_colaborador()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);

        await Assert.ThrowsAsync<RejectionReasonRequiredException>(() => approvals.Reject(claim.Id, scenario.ApproverEmail, ""));
        var stillPending = await claims.Get(claim.Id);
        Assert.Equal(MileageClaimStatus.Pending, stillPending.Status);

        await approvals.Reject(claim.Id, scenario.ApproverEmail, "Falta el recibo de peaje");

        var rejected = await claims.Get(claim.Id);
        Assert.Equal(MileageClaimStatus.Rejected, rejected.Status);
        Assert.Equal("Falta el recibo de peaje", rejected.RejectionReason);

        var notice = host.Db.Set<NotificationLog>().Single(n => n.Type == NotificationType.Rejected);
        Assert.Equal(scenario.EmployeeEmail, notice.RecipientEmail);
        Assert.Contains("Falta el recibo de peaje", notice.Details);
    }

    /// <summary>RN-12: que la boleta corregida no volviera a Pending, o llegara a finanzas sin nueva aprobación, lo haría fallar.</summary>
    [Fact]
    public async Task Boleta_rechazada_corregida_y_reenviada_vuelve_a_pending_y_requiere_aprobar_de_nuevo()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);
        await approvals.Reject(claim.Id, scenario.ApproverEmail, "Corregir placa");

        var resubmitted = await claims.Submit(claim.Id, scenario.EmployeeNationalId);
        Assert.Equal(MileageClaimStatus.Pending, resubmitted.Status);
        Assert.Null(resubmitted.RejectionReason);

        // No llega a finanzas hasta la nueva aprobación.
        Assert.Empty(await claims.GetApproved());

        await approvals.Approve(claim.Id, scenario.ApproverEmail);
        Assert.Single(await claims.GetApproved());
    }

    /// <summary>RN-15: que una segunda aprobación o un rechazo posterior cambiaran algo lo haría fallar.</summary>
    [Fact]
    public async Task Una_boleta_ya_aprobada_no_se_puede_deshacer()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var approvals = host.Get<IApprovalService>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        await claims.Submit(claim.Id, scenario.EmployeeNationalId);
        await approvals.Approve(claim.Id, scenario.ApproverEmail);

        var afterFirstApproval = await claims.Get(claim.Id);

        // Ni una segunda aprobación...
        await approvals.Approve(claim.Id, scenario.ApproverEmail);
        // ...ni un rechazo posterior, cambian nada.
        await Assert.ThrowsAsync<RejectionReasonRequiredException>(() => approvals.Reject(claim.Id, scenario.ApproverEmail, ""));
        await approvals.Reject(claim.Id, scenario.ApproverEmail, "demasiado tarde");

        var final = await claims.Get(claim.Id);
        Assert.Equal(MileageClaimStatus.Approved, final.Status);
        Assert.Equal(afterFirstApproval.DecidedAt, final.DecidedAt);
        Assert.Null(final.RejectionReason);
    }

    /// <summary>RN-16: que la boleta ya calculada cambiara de tarifa, o que la boleta nueva no usara la tarifa nueva, lo haría fallar.</summary>
    [Fact]
    public async Task Cambiar_la_tarifa_no_afecta_boletas_ya_calculadas()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var rates = host.Get<IRateTableAdmin>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        var withTrip = await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        var originalRate = withTrip.Trips[0].AppliedRatePerKm;

        var entry = (await rates.GetAll()).Single();
        await rates.Update(entry.Id, new RateTableEntry
        {
            VehicleType = entry.VehicleType,
            FuelType = entry.FuelType,
            EngineDisplacementMin = entry.EngineDisplacementMin,
            EngineDisplacementMax = entry.EngineDisplacementMax,
            VehicleAgeYears = entry.VehicleAgeYears,
            RatePerKm = 999m,
        });

        var reread = await claims.Get(claim.Id);
        Assert.Equal(originalRate, reread.Trips[0].AppliedRatePerKm);

        var newClaim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        var newClaimWithTrip = await claims.AddTrip(newClaim.Id, scenario.EmployeeNationalId, new AddTripRequest(new DateOnly(2026, 6, 2), [scenario.StoreAId, scenario.StoreBId]));
        Assert.Equal(999m, newClaimWithTrip.Trips[0].AppliedRatePerKm);
    }
}
