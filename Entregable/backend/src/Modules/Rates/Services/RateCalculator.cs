using MileageClaims.Infrastructure;
using MileageClaims.Modules.Rates.Abstractions;
using MileageClaims.Modules.Rates.Domain;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Rates.Services;

public sealed class RateCalculator : IRateCalculator, IRateTableAdmin
{
    private readonly AppDbContext _db;

    public RateCalculator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RateQuote> CalculateRate(VehicleDeclaration vehicle, DateOnly asOfDate, CancellationToken ct = default)
    {
        var age = Math.Max(0, asOfDate.Year - vehicle.ModelYear);

        var candidates = await _db.Set<RateTableEntry>()
            .Where(r => r.VehicleType == vehicle.VehicleType
                        && r.FuelType == vehicle.FuelType
                        && r.EngineDisplacementMin <= vehicle.EngineDisplacement
                        && r.EngineDisplacementMax >= vehicle.EngineDisplacement)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            throw new RateNotFoundException(vehicle);
        }

        // Exacta si existe; si no, el tramo definido más cercano por debajo (RN-2);
        // si la antigüedad es menor que la fila más chica disponible, se usa esa.
        var exact = candidates.FirstOrDefault(r => r.VehicleAgeYears == age);
        var chosen = exact
                     ?? candidates.Where(r => r.VehicleAgeYears <= age).OrderByDescending(r => r.VehicleAgeYears).FirstOrDefault()
                     ?? candidates.OrderBy(r => r.VehicleAgeYears).First();

        var summary = $"{chosen.VehicleType}, {chosen.FuelType}, {chosen.VehicleAgeYears} años, " +
                      $"{chosen.EngineDisplacementMin}-{chosen.EngineDisplacementMax}cc → {chosen.RatePerKm:C}/km";

        return new RateQuote(chosen.RatePerKm, summary);
    }

    public async Task<IReadOnlyList<RateTableEntry>> GetAll(CancellationToken ct = default) =>
        await _db.Set<RateTableEntry>().OrderBy(r => r.VehicleType).ThenBy(r => r.VehicleAgeYears).ToListAsync(ct);

    public async Task<RateTableEntry> Create(RateTableEntry entry, CancellationToken ct = default)
    {
        entry.Id = Guid.NewGuid();
        _db.Set<RateTableEntry>().Add(entry);
        await _db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<RateTableEntry> Update(Guid id, RateTableEntry entry, CancellationToken ct = default)
    {
        var existing = await _db.Set<RateTableEntry>().FirstOrDefaultAsync(r => r.Id == id, ct)
                       ?? throw new KeyNotFoundException($"RateTableEntry {id} no existe.");

        // Actualiza en el lugar: las boletas ya calculadas guardaron su propio RateQuote
        // congelado (RN-16), así que este cambio no las afecta.
        existing.VehicleType = entry.VehicleType;
        existing.FuelType = entry.FuelType;
        existing.EngineDisplacementMin = entry.EngineDisplacementMin;
        existing.EngineDisplacementMax = entry.EngineDisplacementMax;
        existing.VehicleAgeYears = entry.VehicleAgeYears;
        existing.RatePerKm = entry.RatePerKm;

        await _db.SaveChangesAsync(ct);
        return existing;
    }
}

public sealed class RateNotFoundException(VehicleDeclaration vehicle)
    : Exception($"No hay tarifa vigente para {vehicle.VehicleType}/{vehicle.FuelType} con cilindraje {vehicle.EngineDisplacement}cc.")
{
    public VehicleDeclaration Vehicle { get; } = vehicle;
}
