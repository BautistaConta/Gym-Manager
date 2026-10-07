using GymManager.API.DTOs;
using GymManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.API.Controllers;

[ApiController]
[Route("api/admin/pilot-events")]
[Authorize(Roles = "Admin")]
public sealed class PilotEventsController(PilotEventService service) : ControllerBase
{
    [HttpGet("resumen")]
    public Task<PilotEventSummaryResponse> GetSummary(
        [FromQuery] PilotEventSummaryQuery query,
        CancellationToken cancellationToken) => service.GetSummaryAsync(query, cancellationToken);
}
