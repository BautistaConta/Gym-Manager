using GymManager.API.Models;

namespace GymManager.API.Services;

public interface INotificacionDatos
{
    Task<Alumno?> GetAlumnoAsync(string id);
    Task<Pago?> GetUltimoPagoAsync(string alumnoId);
}
