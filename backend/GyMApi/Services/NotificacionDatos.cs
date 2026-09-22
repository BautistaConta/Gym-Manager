using GymManager.API.Models;
using GymManager.API.Repositories;

namespace GymManager.API.Services;

public sealed class NotificacionDatos(AlumnoRepository alumnos, PagoRepository pagos, SucursalRepository sucursales) : INotificacionDatos
{
    public Task<Alumno?> GetAlumnoAsync(string id) => alumnos.GetByIdAsync(id);
    public Task<Pago?> GetUltimoPagoAsync(string alumnoId) => pagos.GetUltimoPagoAsync(alumnoId);
    public Task<List<Alumno>> GetAlumnosAsync(IEnumerable<string> ids) => alumnos.GetByIdsAsync(ids);
    public Task<List<Sucursal>> GetSucursalesAsync(IEnumerable<string> ids) => sucursales.GetByIdsAsync(ids);
}
