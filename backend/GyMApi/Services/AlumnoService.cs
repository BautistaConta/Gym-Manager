using GymManager.API.DTOs;
using GymManager.API.Models;
using GymManager.API.Repositories;

namespace GymManager.API.Services;

public class AlumnoService
{
    private readonly AlumnoRepository _alumnos;
    private readonly PagoRepository _pagos;
    private readonly SucursalRepository _sucursales;
    private readonly CuotaCalculator _cuotas;

    public AlumnoService(AlumnoRepository alumnos, PagoRepository pagos, SucursalRepository sucursales, CuotaCalculator cuotas)
    {
        _alumnos = alumnos;
        _pagos = pagos;
        _sucursales = sucursales;
        _cuotas = cuotas;
    }

    public async Task<List<AlumnoListadoResponse>> GetAllAsync()
    {
        var alumnos = await _alumnos.GetAllAsync();
        var ultimos = await _pagos.GetUltimosPorAlumnoAsync(alumnos.Select(a => a.Id));
        return alumnos.Select(alumno =>
        {
            var cuota = _cuotas.Evaluar(ultimos.GetValueOrDefault(alumno.Id)?.PeriodoHasta);
            return new AlumnoListadoResponse
            {
                Id = alumno.Id,
                Nombre = alumno.Nombre,
                DNI = alumno.DNI,
                Telefono = alumno.Telefono,
                Activo = alumno.Activo,
                SucursalPrincipalId = alumno.SucursalPrincipalId,
                Estado = cuota.Estado.ToString(),
                FechaVencimiento = cuota.FechaVencimiento
            };
        }).ToList();
    }
    public Task<Alumno?> GetByIdAsync(string id) => _alumnos.GetByIdAsync(id);
    public Task<Alumno?> GetByDniAsync(string dni) => _alumnos.GetByDniAsync(dni.Trim());
    public Task<List<Alumno>> SearchAsync(string nombre) => _alumnos.SearchByNombreAsync(nombre.Trim());

    public async Task<Alumno> CreateAsync(CrearAlumnoRequest request)
    {
        Validate(request.Nombre, request.DNI, request.Telefono);
        NotificacionService.ValidatePhone(request.Telefono);
        var dni = request.DNI.Trim();
        if (await _alumnos.GetByDniAsync(dni) is not null)
            throw new DomainException("Ya existe un alumno con ese DNI.");

        var sucursalPrincipalId = await ValidateSucursalPrincipalAsync(request.SucursalPrincipalId);

        var alumno = new Alumno
        {
            Nombre = request.Nombre.Trim(),
            DNI = dni,
            Telefono = request.Telefono.Trim(),
            FechaAlta = DateTime.UtcNow,
            Activo = true,
            SucursalPrincipalId = sucursalPrincipalId
        };
        await _alumnos.CreateAsync(alumno);
        return alumno;
    }

    public async Task<Alumno> UpdateAsync(string id, ActualizarAlumnoRequest request)
    {
        Validate(request.Nombre, "0", request.Telefono);
        NotificacionService.ValidatePhone(request.Telefono);
        var alumno = await _alumnos.GetByIdAsync(id) ?? throw new DomainException("Alumno no encontrado.");
        alumno.Nombre = request.Nombre.Trim();
        alumno.Telefono = request.Telefono.Trim();
        alumno.Activo = request.Activo;
        alumno.SucursalPrincipalId = await ValidateSucursalPrincipalAsync(request.SucursalPrincipalId);
        await _alumnos.UpdateAsync(alumno);
        return alumno;
    }

    public async Task DeactivateAsync(string id)
    {
        var alumno = await _alumnos.GetByIdAsync(id) ?? throw new DomainException("Alumno no encontrado.");
        alumno.Activo = false;
        await _alumnos.UpdateAsync(alumno);
    }

    public async Task<Alumno> ActualizarNotificacionesAsync(string id, bool habilitadas)
    {
        var alumno = await _alumnos.GetByIdAsync(id) ?? throw new DomainException("Alumno no encontrado.");
        alumno.NotificacionesHabilitadas = habilitadas;
        await _alumnos.UpdateAsync(alumno);
        return alumno;
    }

    public async Task<EstadoAlumnoResponse> GetEstadoAsync(string id)
    {
        var alumno = await _alumnos.GetByIdAsync(id) ?? throw new DomainException("Alumno no encontrado.");
        var ultimoPago = await _pagos.GetUltimoPagoAsync(id);
        var cuota = _cuotas.Evaluar(ultimoPago?.PeriodoHasta);
        return new EstadoAlumnoResponse
        {
            AlumnoId = alumno.Id,
            Nombre = alumno.Nombre,
            DNI = alumno.DNI,
            Estado = cuota.Estado.ToString(),
            FechaVencimiento = cuota.FechaVencimiento
        };
    }

    private async Task<string?> ValidateSucursalPrincipalAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var normalized = id.Trim();
        if (await _sucursales.GetByIdAsync(normalized) is null)
            throw new DomainException("La sucursal principal no existe en el gimnasio actual.");
        return normalized;
    }

    private static void Validate(string nombre, string dni, string telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(dni) || string.IsNullOrWhiteSpace(telefono))
            throw new DomainException("Nombre, DNI y teléfono son obligatorios.");
    }
}
