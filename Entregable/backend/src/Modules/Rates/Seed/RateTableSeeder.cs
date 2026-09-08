using MileageClaims.Infrastructure;
using MileageClaims.Modules.Rates.Domain;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Rates.Seed;

/// <summary>
/// Datos sintéticos de la tabla de tarifas para poder probar el recorrido de punta a punta
/// sin la UI de administrador (esa es la Pieza 3). No son datos reales de la compañía.
/// </summary>
public static class RateTableSeeder
{
    public static async Task SeedIfEmpty(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Set<RateTableEntry>().AnyAsync(ct)) return;

        var entries = new List<RateTableEntry>();
        foreach (var vehicleType in new[] { VehicleType.Car, VehicleType.Motorcycle })
        foreach (var fuelType in new[] { FuelType.Gasoline, FuelType.Diesel, FuelType.Hybrid, FuelType.Electric })
        {
            var (min, max) = vehicleType == VehicleType.Car ? (1000, 3000) : (100, 1000);
            var baseRate = vehicleType == VehicleType.Car ? 220m : 110m;

            for (var age = 0; age <= 10; age++)
            {
                // La tarifa baja levemente con la antigüedad; el tramo 10 es el tope (RN-2).
                var rate = Math.Round(baseRate - age * 4m, 2);
                entries.Add(new RateTableEntry
                {
                    Id = Guid.NewGuid(),
                    VehicleType = vehicleType,
                    FuelType = fuelType,
                    EngineDisplacementMin = min,
                    EngineDisplacementMax = max,
                    VehicleAgeYears = age,
                    RatePerKm = rate
                });
            }
        }

        db.Set<RateTableEntry>().AddRange(entries);
        await db.SaveChangesAsync(ct);
    }
}
