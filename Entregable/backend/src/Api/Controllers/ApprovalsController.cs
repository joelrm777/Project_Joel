using MileageClaims.Api.Auth;
using MileageClaims.Modules.Approvals.Abstractions;
using MileageClaims.Modules.Claims.Abstractions;
using MileageClaims.Modules.Claims.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MileageClaims.Api.Controllers;

public sealed record RejectRequest(string Reason);

[ApiController]
[Route("api/approvals")]
[Authorize(Roles = "Approver")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _approvals;
    private readonly IMileageClaimService _claims;

    public ApprovalsController(IApprovalService approvals, IMileageClaimService claims)
    {
        _approvals = approvals;
        _claims = claims;
    }

    private string ApproverEmail => User.GetEmail()!;

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<MileageClaimDto>>> Pending(CancellationToken ct) =>
        Ok(await _claims.GetForApprover(ApproverEmail, ct));

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await _approvals.Approve(id, ApproverEmail, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectRequest request, CancellationToken ct)
    {
        await _approvals.Reject(id, ApproverEmail, request.Reason, ct);
        return NoContent();
    }
}
