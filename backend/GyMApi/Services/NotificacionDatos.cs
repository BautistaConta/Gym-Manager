using GymManager.API.Models;
using GymManager.API.Repositories;

namespace GymManager.API.Services;

public sealed class NotificacionDatos(AlumnoRepository alumnos, PagoRepository pagos) : INotificacionDatos
{
    public Task<Alumno?> GetAlumnoAsync(string id) => alumnos.GetByIdAsync(id);
    public Task<Pago?> GetUltimoPagoAsync(string alumnoId) => pagos.GetUltimoPagoAsync(alumnoId);
}
