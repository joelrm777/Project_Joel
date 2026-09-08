using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Claims.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MileageClaims.Api.Controllers;

[ApiController]
[Route("api/finance")]
[Authorize(Roles = "Finance")]
public sealed class FinanceController : ControllerBase
{
    private readonly IMileageClaimService _claims;

    public FinanceController(IMileageClaimService claims)
    {
        _claims = claims;
    }

    [HttpGet("mileage-claims")]
    public async Task<ActionResult<IReadOnlyList<MileageClaimDto>>> GetApproved(CancellationToken ct) =>
        Ok(await _claims.GetApproved(ct));
}
