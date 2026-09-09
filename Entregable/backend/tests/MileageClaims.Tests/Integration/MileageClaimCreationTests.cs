using MileageClaims.Modules.Claims;
using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Claims.Dtos;
using MileageClaims.Modules.Rates.Domain;
using MileageClaims.Tests.Infrastructure;
using DriveType = MileageClaims.Modules.Claims.Domain.DriveType;
using MileageClaim = MileageClaims.Modules.Claims.Domain.MileageClaim;

namespace MileageClaims.Tests.Integration;

public sealed class MileageClaimCreationTests
{
    private static CreateMileageClaimRequest Vehicle(DriveType driveType = DriveType.Single) =>
        new(VehicleType.Car, FuelType.Gasoline, "ABC123", driveType, 2026, 1500);

    /// <summary>RN-3: que el cálculo tomara en cuenta DriveType lo haría fallar.</summary>
    [Fact]
    public async Task La_traccion_no_cambia_el_costo_por_km()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();

        var claimSingle = await claims.Create(scenario.EmployeeNationalId, Vehicle(DriveType.Single));
        var tripSingle = await claims.AddTrip(claimSingle.Id, scenario.EmployeeNationalId,
            new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));

        var claimDouble = await claims.Create(scenario.EmployeeNationalId, Vehicle(DriveType.DoubleTraction));
        var tripDouble = await claims.AddTrip(claimDouble.Id, scenario.EmployeeNationalId,
            new AddTripRequest(new DateOnly(2026, 6, 2), [scenario.StoreAId, scenario.StoreBId]));

        Assert.Equal(tripSingle.Trips[0].AppliedRatePerKm, tripDouble.Trips[0].AppliedRatePerKm);
        Assert.Equal(tripSingle.Trips[0].TotalAmount, tripDouble.Trips[0].TotalAmount);
    }

    /// <summary>RN-4: que dos viajes de la misma boleta terminaran con tarifas distintas lo haría fallar.</summary>
    [Fact]
    public async Task Todos_los_viajes_de_una_boleta_usan_el_mismo_vehiculo_y_la_misma_tarifa()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();

        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim.Id, scenario.EmployeeNationalId,
            new AddTripRequest(new DateOnly(2026, 6, 1), [scenario.StoreAId, scenario.StoreBId]));
        var updated = await claims.AddTrip(claim.Id, scenario.EmployeeNationalId,
            new AddTripRequest(new DateOnly(2026, 6, 2), [scenario.StoreBId, scenario.StoreCId]));

        Assert.Equal(2, updated.Trips.Count);
        Assert.Equal(updated.Trips[0].AppliedRatePerKm, updated.Trips[1].AppliedRatePerKm);
        Assert.Equal(updated.Trips[0].TotalAmount + updated.Trips[1].TotalAmount, updated.TotalAmount);
    }

    /// <summary>RN-6: correr el chequeo de ventana contra un borde distinto (ej. solo 1 mes, o sin tope) lo haría fallar.</summary>
    [Theory]
    [InlineData(2026, 4, 1, true)]   // primer día permitido (mes en curso - 2 meses)
    [InlineData(2026, 6, 30, true)]  // último día del mes en curso
    [InlineData(2026, 3, 31, false)] // un día antes del borde permitido
    [InlineData(2026, 7, 1, false)]  // un día después del mes en curso
    public async Task Solo_acepta_viajes_dentro_de_la_ventana_de_mes_en_curso_mas_dos_meses_atras(int year, int month, int day, bool shouldSucceed)
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var claim = await claims.Create(scenario.EmployeeNationalId, Vehicle());

        var request = new AddTripRequest(new DateOnly(year, month, day), [scenario.StoreAId, scenario.StoreBId]);

        if (shouldSucceed)
        {
            var result = await claims.AddTrip(claim.Id, scenario.EmployeeNationalId, request);
            Assert.Single(result.Trips);
        }
        else
        {
            await Assert.ThrowsAsync<TripDateOutOfWindowException>(() => claims.AddTrip(claim.Id, scenario.EmployeeNationalId, request));
        }
    }

    /// <summary>RN-7: dejar de comparar contra otras boletas del mismo colaborador (o compararlo entre colaboradores) lo haría fallar.</summary>
    [Fact]
    public async Task Un_viaje_identico_no_se_repite_en_otra_boleta_ni_en_la_misma_del_mismo_colaborador()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var claims = host.Get<IMileageClaimService>();
        var date = new DateOnly(2026, 6, 1);
        var route = new[] { scenario.StoreAId, scenario.StoreBId };

        var claim1 = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim1.Id, scenario.EmployeeNationalId, new AddTripRequest(date, route));

        // Misma boleta, mismo viaje otra vez.
        await Assert.ThrowsAsync<DuplicateTripException>(
            () => claims.AddTrip(claim1.Id, scenario.EmployeeNationalId, new AddTripRequest(date, route)));

        // Boleta nueva, mismo colaborador, mismo viaje.
        var claim2 = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await Assert.ThrowsAsync<DuplicateTripException>(
            () => claims.AddTrip(claim2.Id, scenario.EmployeeNationalId, new AddTripRequest(date, route)));
    }

    /// <summary>Control: el mismo viaje de OTRO colaborador no debe chocar — si esto empezara a fallar, la comparación de RN-7 se volvió demasiado ancha.</summary>
    [Fact]
    public async Task El_mismo_viaje_de_otro_colaborador_no_choca_con_RN_7()
    {
        using var host = TestHost.Create();
        var scenario = TestData.SeedStandardScenario(host.Db);
        var otherEmployee = TestData.SeedEmployee(host.Db, "333333333", "222222222", scenario.ApproverEmail);
        var claims = host.Get<IMileageClaimService>();
        var date = new DateOnly(2026, 6, 1);
        var route = new[] { scenario.StoreAId, scenario.StoreBId };

        var claim1 = await claims.Create(scenario.EmployeeNationalId, Vehicle());
        await claims.AddTrip(claim1.Id, scenario.EmployeeNationalId, new AddTripRequest(date, route));

        var claim2 = await claims.Create(otherEmployee.NationalId, Vehicle());
        var result = await claims.AddTrip(claim2.Id, otherEmployee.NationalId, new AddTripRequest(date, route));

        Assert.Single(result.Trips);
    }

    /// <summary>RN-17: que se creara la boleta igual, o que quedara alguna fila en la base, lo haría fallar.</summary>
    [Fact]
    public async Task Cedula_no_reconocida_por_el_ERP_no_crea_ninguna_boleta()
    {
        using var host = TestHost.Create();
        TestData.SeedStandardScenario(host.Db); // no incluye la cédula "999999999"
        var claims = host.Get<IMileageClaimService>();

        await Assert.ThrowsAsync<EmployeeNotFoundException>(() => claims.Create("999999999", Vehicle()));

        Assert.Empty(host.Db.Set<MileageClaim>());
    }

    /// <summary>RN-17: una cédula que existe pero está inactiva se trata igual que si no existiera.</summary>
    [Fact]
    public async Task Cedula_de_colaborador_inactivo_no_crea_ninguna_boleta()
    {
        using var host = TestHost.Create();
        TestData.SeedStandardScenario(host.Db);
        var inactive = TestData.SeedEmployee(host.Db, "444444444", "222222222", "222222222@automercado.test", isActive: false);
        var claims = host.Get<IMileageClaimService>();

        await Assert.ThrowsAsync<EmployeeNotFoundException>(() => claims.Create(inactive.NationalId, Vehicle()));

        Assert.Empty(host.Db.Set<MileageClaim>());
    }
}
