using GymManager.API.DTOs;
using GymManager.API.Models;
using GymManager.API.Repositories;

namespace GymManager.API.Services;

public class AlumnoService
{
    private readonly AlumnoRepository _alumnos;
    private readonly PagoRepository _pagos;

    public AlumnoService(AlumnoRepository alumnos, PagoRepository pagos)
    {
        _alumnos = alumnos;
        _pagos = pagos;
    }

    public Task<List<Alumno>> GetAllAsync() => _alumnos.GetAllAsync();
    public Task<Alumno?> GetByIdAsync(string id) => _alumnos.GetByIdAsync(id);
    public Task<Alumno?> GetByDniAsync(string dni) => _alumnos.GetByDniAsync(dni.Trim());
    public Task<List<Alumno>> SearchAsync(string nombre) => _alumnos.SearchByNombreAsync(nombre.Trim());

    public async Task<Alumno> CreateAsync(CrearAlumnoRequest request)
    {
        Validate(request.Nombre, request.DNI, request.Telefono);
        var dni = request.DNI.Trim();
        if (await _alumnos.GetByDniAsync(dni) is not null)
            throw new DomainException("Ya existe un alumno con ese DNI.");

        var alumno = new Alumno
        {
            Nombre = request.Nombre.Trim(),
            DNI = dni,
            Telefono = request.Telefono.Trim(),
            FechaAlta = DateTime.UtcNow,
            Activo = true
        };
        await _alumnos.CreateAsync(alumno);
        return alumno;
    }

    public async Task<Alumno> UpdateAsync(string id, ActualizarAlumnoRequest request)
    {
        Validate(request.Nombre, "0", request.Telefono);
        var alumno = await _alumnos.GetByIdAsync(id) ?? throw new DomainException("Alumno no encontrado.");
        alumno.Nombre = request.Nombre.Trim();
        alumno.Telefono = request.Telefono.Trim();
        alumno.Activo = request.Activo;
        await _alumnos.UpdateAsync(alumno);
        return alumno;
    }

    public async Task DeactivateAsync(string id)
    {
        var alumno = await _alumnos.GetByIdAsync(id) ?? throw new DomainException("Alumno no encontrado.");
        alumno.Activo = false;
        await _alumnos.UpdateAsync(alumno);
    }

    public async Task<EstadoAlumnoResponse> GetEstadoAsync(string id)
    {
        var alumno = await _alumnos.GetByIdAsync(id) ?? throw new DomainException("Alumno no encontrado.");
        var ultimoPago = await _pagos.GetUltimoPagoAsync(id);
        var vencimiento = ultimoPago?.PeriodoHasta;
        return new EstadoAlumnoResponse
        {
            AlumnoId = alumno.Id,
            Nombre = alumno.Nombre,
            DNI = alumno.DNI,
            Estado = vencimiento is null ? "SIN_PAGOS" : vencimiento.Value.Date >= DateTime.UtcNow.Date ? "ACTIVO" : "VENCIDO",
            FechaVencimiento = vencimiento
        };
    }

    private static void Validate(string nombre, string dni, string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(dni) || string.IsNullOrWhiteSpace(telefono))
            throw new DomainException("Nombre, DNI y teléfono son obligatorios.");
    }
}
