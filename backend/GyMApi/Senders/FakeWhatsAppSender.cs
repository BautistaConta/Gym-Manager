using GymManager.API.Models;

namespace GymManager.API.Senders;

// No hace HTTP ni finge una aceptación del proveedor.
public sealed class FakeWhatsAppSender : IWhatsAppSender
{
    public Task<WhatsAppSendResult> SendAsync(NotificacionWhatsApp notificacion, CancellationToken cancellationToken = default) =>
        Task.FromResult(new WhatsAppSendResult(false, "Simulación local: no se contactó Twilio.", ResultadoDefinitivo: true, Simulado: true));
}
