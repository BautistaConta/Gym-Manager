using GymManager.API.Models;

namespace GymManager.API.Senders;

public interface IWhatsAppSender
{
    Task<WhatsAppSendResult> SendAsync(NotificacionWhatsApp notificacion, CancellationToken cancellationToken = default);
}

public record WhatsAppSendResult(bool Exitoso, string? ErrorDetalle = null,
    string? ProviderMessageId = null, bool ResultadoDefinitivo = false);
