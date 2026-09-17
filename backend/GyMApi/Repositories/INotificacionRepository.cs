using GymManager.API.Models;

namespace GymManager.API.Repositories;

public interface INotificacionRepository
{
    Task<NotificacionWhatsApp> CreateIfAbsentAsync(NotificacionWhatsApp notificacion);
    Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId);
    Task<NotificacionWhatsApp?> GetByIdAsync(string id);
    Task<List<NotificacionWhatsApp>> GetByCampaniaAsync(string campaniaId);
    Task<NotificacionWhatsApp?> ClaimNextAsync(DateTime nowUtc);
    Task<bool> TransitionAsync(string id, EstadoNotificacionWhatsApp expected, EstadoNotificacionWhatsApp next,
        DateTime nowUtc, string? error = null, string? providerMessageId = null);
    Task<long> MarkProcessingForReviewAsync(DateTime nowUtc);
    Task<long> CancelPendingByCampaniaAsync(string campaniaId, DateTime nowUtc);
}
