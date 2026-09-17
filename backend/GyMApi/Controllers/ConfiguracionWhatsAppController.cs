using GymManager.API.DTOs;
using GymManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.API.Controllers;

[ApiController, Route("api/configuracion/whatsapp"), Authorize(Roles = "Admin")]
public sealed class ConfiguracionWhatsAppController(ConfiguracionGymService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get() => Ok(await service.GetAsync());
    [HttpPut] public async Task<IActionResult> Update(ConfiguracionWhatsAppRequest request)
    {
        try { return Ok(await service.UpdateAsync(request)); }
        catch (DomainException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
