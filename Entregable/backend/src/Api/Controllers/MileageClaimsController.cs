using MileageClaims.Api.Auth;
using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Claims.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MileageClaims.Api.Controllers;

[ApiController]
[Route("api/mileage-claims")]
[Authorize(Roles = "Employee")]
public sealed class MileageClaimsController : ControllerBase
{
    private readonly IMileageClaimService _claims;

    public MileageClaimsController(IMileageClaimService claims)
    {
        _claims = claims;
    }

    private string NationalId => User.GetNationalId()!;

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<MileageClaimDto>>> Mine(CancellationToken ct) =>
        Ok(await _claims.GetForEmployee(NationalId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MileageClaimDto>> Get(Guid id, CancellationToken ct)
    {
        var claim = await _claims.Get(id, ct);
        if (claim.EmployeeNationalId != NationalId) return Forbid();
        return Ok(claim);
    }

    [HttpPost]
    public async Task<ActionResult<MileageClaimDto>> Create(CreateMileageClaimRequest request, CancellationToken ct)
    {
        var claim = await _claims.Create(request with { EmployeeNationalId = NationalId }, ct);
        return CreatedAtAction(nameof(Get), new { id = claim.Id }, claim);
    }

    [HttpPut("{id:guid}/vehicle")]
    public async Task<ActionResult<MileageClaimDto>> UpdateVehicle(Guid id, UpdateVehicleRequest request, CancellationToken ct) =>
        Ok(await _claims.UpdateVehicle(id, NationalId, request, ct));

    [HttpPost("{id:guid}/trips")]
    public async Task<ActionResult<MileageClaimDto>> AddTrip(Guid id, AddTripRequest request, CancellationToken ct) =>
        Ok(await _claims.AddTrip(id, NationalId, request, ct));

    [HttpDelete("{id:guid}/trips/{tripId:guid}")]
    public async Task<ActionResult<MileageClaimDto>> RemoveTrip(Guid id, Guid tripId, CancellationToken ct) =>
        Ok(await _claims.RemoveTrip(id, NationalId, tripId, ct));

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<MileageClaimDto>> Submit(Guid id, CancellationToken ct) =>
        Ok(await _claims.Submit(id, NationalId, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken ct)
    {
        await _claims.Withdraw(id, NationalId, ct);
        return NoContent();
    }
}
