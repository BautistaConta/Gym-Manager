using GymManager.API.Models;

namespace GymManager.API.Services;

public interface INotificacionDatos
{
    Task<Alumno?> GetAlumnoAsync(string id);
    Task<Pago?> GetUltimoPagoAsync(string alumnoId);
    Task<List<Alumno>> GetAlumnosAsync(IEnumerable<string> ids);
    Task<List<Sucursal>> GetSucursalesAsync(IEnumerable<string> ids);
}
