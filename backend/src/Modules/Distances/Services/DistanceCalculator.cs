using MileageClaims.Infrastructure;
using MileageClaims.Modules.Distances.Abstractions;
using MileageClaims.Modules.Distances.Domain;
using Microsoft.EntityFrameworkCore;

namespace MileageClaims.Modules.Distances.Services;

public sealed class DistanceCalculator : IDistanceCalculator, IStoreAdmin, IStoreDistanceAdmin
{
    private readonly AppDbContext _db;

    public DistanceCalculator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DistanceResult> CalculateTotalDistance(IReadOnlyList<int> storeIdsInOrder, CancellationToken ct = default)
    {
        if (storeIdsInOrder.Count < 2)
        {
            throw new ArgumentException("Un viaje necesita al menos dos tiendas.", nameof(storeIdsInOrder));
        }

        var legs = new List<LegAttempt>();
        decimal total = 0m;

        for (var i = 0; i < storeIdsInOrder.Count - 1; i++)
        {
            var origin = storeIdsInOrder[i];
            var destination = storeIdsInOrder[i + 1];

            var row = await _db.Set<StoreDistance>().FirstOrDefaultAsync(
                d => (d.OriginStoreId == origin && d.DestinationStoreId == destination)
                     || (d.OriginStoreId == destination && d.DestinationStoreId == origin),
                ct);

            legs.Add(new LegAttempt(i + 1, origin, destination, row?.DistanceKm));
            if (row is not null)
            {
                total += row.DistanceKm;
            }
        }

        var isComplete = legs.All(l => l.DistanceKm.HasValue);
        return new DistanceResult(isComplete, total, legs);
    }

    public async Task<IReadOnlyList<Store>> GetAll(CancellationToken ct = default) =>
        await _db.Set<Store>().OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<Store> Create(string name, CancellationToken ct = default)
    {
        var store = new Store { Name = name };
        _db.Set<Store>().Add(store);
        await _db.SaveChangesAsync(ct);
        return store;
    }

    async Task<IReadOnlyList<StoreDistance>> IStoreDistanceAdmin.GetAll(CancellationToken ct) =>
        await _db.Set<StoreDistance>().ToListAsync(ct);

    public async Task<StoreDistance> Upsert(int originStoreId, int destinationStoreId, decimal distanceKm, CancellationToken ct = default)
    {
        var existing = await _db.Set<StoreDistance>().FirstOrDefaultAsync(
            d => (d.OriginStoreId == originStoreId && d.DestinationStoreId == destinationStoreId)
                 || (d.OriginStoreId == destinationStoreId && d.DestinationStoreId == originStoreId),
            ct);

        if (existing is not null)
        {
            existing.DistanceKm = distanceKm;
        }
        else
        {
            existing = new StoreDistance { OriginStoreId = originStoreId, DestinationStoreId = destinationStoreId, DistanceKm = distanceKm };
            _db.Set<StoreDistance>().Add(existing);
        }

        await _db.SaveChangesAsync(ct);
        return existing;
    }
}
