using GymManager.API.Models;
using GymManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymManager.API.DTOs;

namespace GymManager.API.Controllers;

[ApiController]
[Route("api/notificaciones")]
[Authorize(Roles = "Admin,Gestor")]
public class NotificacionController : ControllerBase
{
    private readonly NotificacionService _service;
    public NotificacionController(NotificacionService service) => _service = service;
    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] NotificacionHistorialQuery query) => await ExecuteAsync(() => _service.GetHistorialAsync(query));
    [HttpPost("reenviar/{id}")] public async Task<IActionResult> Reenviar(string id, ReintentarNotificacionRequest request)
    {
        if (!request.ConfirmacionExplicita) return BadRequest(new { message = "Confirmá explícitamente el reintento y sus posibles cargos." });
        return await ExecuteAsync(() => _service.ReenviarAsync(id));
    }
    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (DomainException ex) { return BadRequest(new { message = ex.Message }); } }
}

public sealed class ReintentarNotificacionRequest
{
    public bool ConfirmacionExplicita { get; set; }
}
