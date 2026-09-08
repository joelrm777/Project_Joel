using MileageClaims.Modules.Distances.Abstractions;
using MileageClaims.Modules.Distances.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MileageClaims.Api.Controllers;

/// <summary>Catálogo de tiendas de solo lectura — cualquier rol autenticado lo necesita para armar un viaje.</summary>
[ApiController]
[Route("api/stores")]
[Authorize]
public sealed class StoresController : ControllerBase
{
    private readonly IStoreAdmin _stores;

    public StoresController(IStoreAdmin stores)
    {
        _stores = stores;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Store>>> GetAll(CancellationToken ct) =>
        Ok(await _stores.GetAll(ct));
}
