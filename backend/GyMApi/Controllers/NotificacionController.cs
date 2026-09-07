using GymManager.API.Models;
using GymManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.API.Controllers;

[ApiController]
[Route("api/notificaciones")]
[Authorize(Roles = "Admin,Gestor")]
public class NotificacionController : ControllerBase
{
    private readonly NotificacionService _service;
    public NotificacionController(NotificacionService service) => _service = service;
    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] EstadoNotificacionWhatsApp? estado, [FromQuery] string? alumnoId) => Ok(await _service.GetAllAsync(estado, alumnoId));
    [HttpGet("{id}")] public async Task<IActionResult> GetById(string id) => await _service.GetByIdAsync(id) is { } notificacion ? Ok(notificacion) : NotFound();
    [HttpPost("reenviar/{id}")] public async Task<IActionResult> Reenviar(string id) => await ExecuteAsync(() => _service.ReenviarAsync(id));
    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (DomainException ex) { return BadRequest(new { message = ex.Message }); } }
}
