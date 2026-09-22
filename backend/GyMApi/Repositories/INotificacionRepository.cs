using GymManager.API.Models;
using GymManager.API.DTOs;

namespace GymManager.API.Repositories;

public interface INotificacionRepository
{
    Task<NotificacionWhatsApp> CreateIfAbsentAsync(NotificacionWhatsApp notificacion);
    Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId);
    Task<NotificacionWhatsApp?> GetByIdAsync(string id);
    Task<NotificacionPageData> SearchAsync(NotificacionHistorialQuery query);
    Task<List<NotificacionWhatsApp>> GetByCampaniaAsync(string campaniaId);
    Task<NotificacionWhatsApp?> ClaimNextAsync(DateTime nowUtc);
    Task<bool> TransitionAsync(string id, EstadoNotificacionWhatsApp expected, EstadoNotificacionWhatsApp next,
        DateTime nowUtc, string? error = null, string? providerMessageId = null);
    Task<long> MarkProcessingForReviewAsync(DateTime nowUtc);
    Task<long> CancelPendingByCampaniaAsync(string campaniaId, DateTime nowUtc);
}
