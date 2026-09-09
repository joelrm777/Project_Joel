using MileageClaims.Modules.Rates.Abstractions;
using MileageClaims.Modules.Rates.Domain;
using MileageClaims.Tests.Infrastructure;

namespace MileageClaims.Tests.Unit;

/// <summary>
/// RN-1, RN-2: el costo por km sale siempre de la tabla vigente, cruzando tipo, combustible,
/// cilindraje y antigüedad exacta; la antigüedad que supera el tramo más viejo definido usa
/// ese último tramo. Nivel unidad: es una regla de cálculo con casos borde, cara de probar
/// por HTTP o por el servicio completo de boletas.
/// </summary>
public sealed class RateCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 6, 15);

    /// <summary>Cambiar la fórmula de cruce (tipo/combustible/cilindraje/antigüedad) por otra distinta la haría fallar.</summary>
    [Fact]
    public async Task Usa_la_tarifa_de_la_fila_exacta_de_tipo_combustible_cilindraje_y_antiguedad()
    {
        using var host = TestHost.Create();
        TestData.SeedRate(host.Db, VehicleType.Car, FuelType.Gasoline, 1000, 3000, ageYears: 3, ratePerKm: 208m);
        TestData.SeedRate(host.Db, VehicleType.Car, FuelType.Gasoline, 1000, 3000, ageYears: 4, ratePerKm: 204m);

        var calculator = host.Get<IRateCalculator>();
        // Modelo 2022 → antigüedad 4 en 2026 (Today).
        var quote = await calculator.CalculateRate(new VehicleDeclaration(VehicleType.Car, FuelType.Gasoline, 1500, 2022), Today);

        Assert.Equal(204m, quote.RatePerKm);
    }

    /// <summary>Que el cálculo tomara la fila de otro combustible o tipo de vehículo lo haría fallar.</summary>
    [Fact]
    public async Task No_mezcla_tarifas_de_otro_combustible_ni_otro_tipo_de_vehiculo()
    {
        using var host = TestHost.Create();
        TestData.SeedRate(host.Db, VehicleType.Car, FuelType.Gasoline, 1000, 3000, ageYears: 0, ratePerKm: 220m);
        TestData.SeedRate(host.Db, VehicleType.Car, FuelType.Diesel, 1000, 3000, ageYears: 0, ratePerKm: 999m);
        TestData.SeedRate(host.Db, VehicleType.Motorcycle, FuelType.Gasoline, 100, 1000, ageYears: 0, ratePerKm: 111m);

        var calculator = host.Get<IRateCalculator>();
        var quote = await calculator.CalculateRate(new VehicleDeclaration(VehicleType.Car, FuelType.Gasoline, 1500, Today.Year), Today);

        Assert.Equal(220m, quote.RatePerKm);
    }

    /// <summary>Quitar el "tope al último tramo" y dejar que la búsqueda falle (o use otra fila) para antigüedades fuera de rango la haría fallar.</summary>
    [Fact]
    public async Task Antiguedad_mayor_a_la_mas_vieja_definida_usa_la_tarifa_del_ultimo_tramo()
    {
        using var host = TestHost.Create();
        TestData.SeedRate(host.Db, VehicleType.Car, FuelType.Gasoline, 1000, 3000, ageYears: 9, ratePerKm: 184m);
        TestData.SeedRate(host.Db, VehicleType.Car, FuelType.Gasoline, 1000, 3000, ageYears: 10, ratePerKm: 180m);

        var calculator = host.Get<IRateCalculator>();
        // Modelo 2000 → antigüedad 26, muy por encima del tramo más viejo definido (10).
        var quote = await calculator.CalculateRate(new VehicleDeclaration(VehicleType.Car, FuelType.Gasoline, 1500, 2000), Today);

        Assert.Equal(180m, quote.RatePerKm);
    }

    /// <summary>Que el cilindraje cayera fuera del rango [min,max] configurado y aun así encontrara una tarifa la haría fallar.</summary>
    [Fact]
    public async Task Sin_ninguna_fila_para_el_cilindraje_declarado_no_hay_tarifa()
    {
        using var host = TestHost.Create();
        TestData.SeedRate(host.Db, VehicleType.Car, FuelType.Gasoline, 1000, 1500, ageYears: 0, ratePerKm: 220m);

        var calculator = host.Get<IRateCalculator>();

        await Assert.ThrowsAsync<MileageClaims.Modules.Rates.Services.RateNotFoundException>(
            () => calculator.CalculateRate(new VehicleDeclaration(VehicleType.Car, FuelType.Gasoline, 2500, Today.Year), Today));
    }
}
