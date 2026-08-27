using GymManager.API.DTOs;
using GymManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.API.Controllers;

[ApiController]
[Route("api/alumnos")]
[Authorize(Roles = "Admin,Gestor")]
public class AlumnosController : ControllerBase
{
    private readonly AlumnoService _service;
    public AlumnosController(AlumnoService service) => _service = service;
    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());
    [HttpGet("{id}")] public async Task<IActionResult> GetById(string id) => await _service.GetByIdAsync(id) is { } alumno ? Ok(alumno) : NotFound();
    [HttpGet("dni/{dni}")] public async Task<IActionResult> GetByDni(string dni) => await _service.GetByDniAsync(dni) is { } alumno ? Ok(alumno) : NotFound();
    [HttpGet("search")] public async Task<IActionResult> SearchByNombre([FromQuery] string nombre) => string.IsNullOrWhiteSpace(nombre) ? BadRequest(new { message = "El nombre es obligatorio." }) : Ok(await _service.SearchAsync(nombre));
    [HttpGet("{id}/estado")] public async Task<IActionResult> GetEstado(string id) => await ExecuteAsync(() => _service.GetEstadoAsync(id));
    [HttpPost] public async Task<IActionResult> Create(CrearAlumnoRequest request) => await ExecuteAsync(() => _service.CreateAsync(request), value => CreatedAtAction(nameof(GetById), new { id = value.Id }, value));
    [HttpPut("{id}")] public async Task<IActionResult> Update(string id, ActualizarAlumnoRequest request) => await ExecuteAsync(() => _service.UpdateAsync(id, request));
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(string id) { try { await _service.DeactivateAsync(id); return NoContent(); } catch (DomainException ex) { return NotFound(new { message = ex.Message }); } }
    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action, Func<T, IActionResult>? success = null) { try { var result = await action(); return success?.Invoke(result) ?? Ok(result); } catch (DomainException ex) { return BadRequest(new { message = ex.Message }); } }
}
