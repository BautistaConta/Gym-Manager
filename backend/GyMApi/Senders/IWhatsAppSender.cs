using GymManager.API.Models;

namespace GymManager.API.Senders;

public interface IWhatsAppSender
{
    Task<WhatsAppSendResult> SendAsync(string telefono, string mensaje, TipoNotificacionWhatsApp tipo, CancellationToken cancellationToken = default);
}

public record WhatsAppSendResult(bool Exitoso, string? ErrorDetalle = null,
    string? ProviderMessageId = null, bool ResultadoDefinitivo = false);
