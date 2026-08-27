using GymManager.API.DTOs;
using GymManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.API.Controllers;

[ApiController]
[Route("api/sucursales")]
[Authorize(Roles = "Admin,Gestor")]
public class SucursalesController : ControllerBase
{
    private readonly SucursalService _service;
    public SucursalesController(SucursalService service) => _service = service;
    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());
    [HttpDelete("sin-id")] public async Task<IActionResult> DeleteLegacyWithoutId() { await _service.DeleteLegacyWithoutIdAsync(); return NoContent(); }
    [HttpGet("{id}")] public async Task<IActionResult> GetById(string id) => await _service.GetByIdAsync(id) is { } sucursal ? Ok(sucursal) : NotFound();
    [HttpPost] public async Task<IActionResult> Create(CrearSucursalRequest request) => await ExecuteAsync(() => _service.CreateAsync(request), value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));
    [HttpPut("{id}")] public async Task<IActionResult> Update(string id, ActualizarSucursalRequest request) => await ExecuteAsync(() => _service.UpdateAsync(id, request));
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(string id) { try { await _service.DeleteAsync(id); return NoContent(); } catch (DomainException ex) { return BadRequest(new { message = ex.Message }); } }
    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action, Func<T, IActionResult>? success = null) { try { var result = await action(); return success?.Invoke(result) ?? Ok(result); } catch (DomainException ex) { return BadRequest(new { message = ex.Message }); } }
}
