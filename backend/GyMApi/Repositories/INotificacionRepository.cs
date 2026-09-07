using GymManager.API.Models;

namespace GymManager.API.Repositories;

public interface INotificacionRepository
{
    Task CreateAsync(NotificacionWhatsApp notificacion);
    Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId);
    Task<NotificacionWhatsApp?> GetByIdAsync(string id);
    Task<bool> ExistsSinceAsync(string alumnoId, TipoNotificacionWhatsApp tipo, DateTime desde);
    Task<bool> ExistsEnviadaDesdeAsync(string alumnoId, TipoNotificacionWhatsApp tipo, DateTime desde);
    Task UpdateAsync(NotificacionWhatsApp notificacion);
}
