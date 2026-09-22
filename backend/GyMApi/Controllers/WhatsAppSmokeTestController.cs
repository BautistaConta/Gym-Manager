using GymManager.API.Models;
using GymManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.API.Controllers;

[ApiController, Route("api/admin/whatsapp/prueba"), Authorize(Roles = "Admin")]
public sealed class WhatsAppSmokeTestController(WhatsAppSmokeTestService service) : ControllerBase
{
    [HttpGet] public IActionResult Readiness() => Ok(new { intervencionesPendientes = service.IntervencionesPendientes() });
    [HttpPost] public async Task<IActionResult> Enviar(PruebaWhatsAppRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await service.EnviarAsync(request.Tipo, request.ConfirmacionCargos, cancellationToken)); }
        catch (DomainException e) { return BadRequest(new { message = e.Message }); }
    }
}
public sealed class PruebaWhatsAppRequest
{
    public TipoNotificacionWhatsApp Tipo { get; set; }
    public bool ConfirmacionCargos { get; set; }
}
