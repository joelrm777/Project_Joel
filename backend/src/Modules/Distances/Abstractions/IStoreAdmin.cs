using MileageClaims.Modules.Distances.Domain;

namespace MileageClaims.Modules.Distances.Abstractions;

public interface IStoreAdmin
{
    Task<IReadOnlyList<Store>> GetAll(CancellationToken ct = default);
    Task<Store> Create(string name, CancellationToken ct = default);
}

public interface IStoreDistanceAdmin
{
    Task<IReadOnlyList<StoreDistance>> GetAll(CancellationToken ct = default);
    Task<StoreDistance> Upsert(int originStoreId, int destinationStoreId, decimal distanceKm, CancellationToken ct = default);
}
