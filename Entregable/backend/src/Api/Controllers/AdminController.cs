using MileageClaims.Modules.Distances.Abstractions;
using MileageClaims.Modules.Distances.Domain;
using MileageClaims.Modules.Rates.Abstractions;
using MileageClaims.Modules.Rates.Domain;
using MileageClaims.Modules.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MileageClaims.Api.Controllers;

public sealed record CreateStoreRequest(string Name);
public sealed record UpsertStoreDistanceRequest(int OriginStoreId, int DestinationStoreId, decimal DistanceKm);

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Administrator")]
public sealed class AdminController : ControllerBase
{
    private readonly IRateTableAdmin _rates;
    private readonly IStoreAdmin _stores;
    private readonly IStoreDistanceAdmin _storeDistances;
    private readonly ClaimLifecycleBackgroundService _timer;

    public AdminController(IRateTableAdmin rates, IStoreAdmin stores, IStoreDistanceAdmin storeDistances, ClaimLifecycleBackgroundService timer)
    {
        _rates = rates;
        _stores = stores;
        _storeDistances = storeDistances;
        _timer = timer;
    }

    /// <summary>
    /// Fuerza un ciclo del Temporizador fuera de su intervalo normal — para poder probar o
    /// demostrar RN-13/RN-14 sin esperar horas. No es parte del recorrido de negocio.
    /// </summary>
    [HttpPost("timer/run-once")]
    public async Task<ActionResult<TimerCycleResult>> RunTimerOnce(CancellationToken ct) =>
        Ok(await _timer.RunOnce(ct));

    [HttpGet("rate-table")]
    public async Task<ActionResult<IReadOnlyList<RateTableEntry>>> GetRateTable(CancellationToken ct) =>
        Ok(await _rates.GetAll(ct));

    [HttpPost("rate-table")]
    public async Task<ActionResult<RateTableEntry>> CreateRateTableEntry(RateTableEntry entry, CancellationToken ct) =>
        Ok(await _rates.Create(entry, ct));

    [HttpPut("rate-table/{id:guid}")]
    public async Task<ActionResult<RateTableEntry>> UpdateRateTableEntry(Guid id, RateTableEntry entry, CancellationToken ct) =>
        Ok(await _rates.Update(id, entry, ct));

    [HttpGet("stores")]
    public async Task<ActionResult<IReadOnlyList<Store>>> GetStores(CancellationToken ct) =>
        Ok(await _stores.GetAll(ct));

    [HttpPost("stores")]
    public async Task<ActionResult<Store>> CreateStore(CreateStoreRequest request, CancellationToken ct) =>
        Ok(await _stores.Create(request.Name, ct));

    [HttpGet("store-distances")]
    public async Task<ActionResult<IReadOnlyList<StoreDistance>>> GetStoreDistances(CancellationToken ct) =>
        Ok(await _storeDistances.GetAll(ct));

    [HttpPut("store-distances")]
    public async Task<ActionResult<StoreDistance>> UpsertStoreDistance(UpsertStoreDistanceRequest request, CancellationToken ct) =>
        Ok(await _storeDistances.Upsert(request.OriginStoreId, request.DestinationStoreId, request.DistanceKm, ct));
}
