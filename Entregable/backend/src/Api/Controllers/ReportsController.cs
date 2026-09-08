using MileageClaims.Modules.Reporting.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MileageClaims.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Finance,Administrator")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportingQueryService _reporting;

    public ReportsController(IReportingQueryService reporting)
    {
        _reporting = reporting;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<SummaryReport>> GetSummary(
        [FromQuery] string? approverEmail,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct) =>
        Ok(await _reporting.GetSummary(approverEmail, from, to, ct));

    [HttpGet("mileage-claims/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var summary = await _reporting.GetById(id, ct);
        return summary is null ? NotFound() : Ok(summary);
    }
}
