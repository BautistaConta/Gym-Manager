using System.Security.Claims;
using GymManager.API.DTOs;
using GymManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.API.Controllers;

[ApiController, Route("api/campanias-whatsapp"), Authorize(Roles = "Admin,Gestor")]
public sealed class CampaniasWhatsAppController(CampaniaWhatsAppService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await service.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> Get(string id) => await Execute(() => service.GetAsync(id));
    [HttpPost] public async Task<IActionResult> Crear(CrearCampaniaRequest request) =>
        await Execute(() => service.CrearAsync(request, UserId(), User.Identity?.Name ?? "Usuario"));
    [HttpPost("{id}/preview")] public async Task<IActionResult> Preview(string id) => await Execute(() => service.PreviewAsync(id));
    [HttpPost("{id}/confirmar")] public async Task<IActionResult> Confirmar(string id, ConfirmarCampaniaRequest request) =>
        await Execute(() => service.ConfirmarAsync(id, request, UserId()));
    [HttpPost("{id}/cancelar")] public async Task<IActionResult> Cancelar(string id) =>
        await Execute(() => service.CancelarAsync(id, UserId()));
    [HttpGet("{id}/reintento-preview")] public async Task<IActionResult> PreviewReintento(string id) =>
        await Execute(() => service.PreviewReintentoAsync(id));
    [HttpPost("{id}/reintentar")] public async Task<IActionResult> Reintentar(string id, ReintentarCampaniaRequest request) =>
        await Execute(() => service.ReintentarFallidasAsync(id, request));

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "desconocido";
    private static async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return new OkObjectResult(await action()); }
        catch (DomainException ex) { return new BadRequestObjectResult(new { message = ex.Message }); }
    }
}
