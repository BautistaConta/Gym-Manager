using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GymManager.API.Models;
using GymManager.API.Options;
using Microsoft.Extensions.Options;

namespace GymManager.API.Senders;

public sealed class TwilioWhatsAppSender(HttpClient httpClient, IOptions<TwilioOptions> options,
    ILogger<TwilioWhatsAppSender> logger) : IWhatsAppSender
{
    private readonly TwilioOptions _options = options.Value;

    public async Task<WhatsAppSendResult> SendAsync(NotificacionWhatsApp n, CancellationToken cancellationToken = default)
    {
        var category = "Configuration";
        if (!_options.Enabled) return Failure("El envío real no está habilitado.");
        if (!_options.PaidAccountConfirmed) return Failure("Falta confirmar manualmente la activación de la cuenta paga.");
        if (!_options.TemplatesApprovedConfirmed) return Failure("Falta confirmar manualmente la aprobación de las plantillas.");
        if (string.IsNullOrWhiteSpace(_options.AccountSid) || string.IsNullOrWhiteSpace(_options.AuthToken) ||
            !NotificacionServicePhone(_options.WhatsAppFromNumber)) return Failure("Faltan secretos o remitente WhatsApp válido.");
        if (!NotificacionServicePhone(n.Telefono)) return Failure("Teléfono inválido.");
        if (n.EsPrueba && (!_options.SmokeTestEnabled || n.Telefono != _options.AuthorizedTestNumber))
            return Failure("Smoke test no habilitado o destinatario no autorizado.");
        if (!n.EsPrueba && !_options.WorkerEnabled) return Failure("El worker real no está habilitado.");
        var sid = n.Tipo switch
        {
            TipoNotificacionWhatsApp.PorVencer => _options.PorVencerContentSid,
            _ => _options.AdditionalTypesEnabled && n.Tipo != TipoNotificacionWhatsApp.PagoConfirmado ? n.ContentSid : null
        };
        if (string.IsNullOrWhiteSpace(sid) || !Regex.IsMatch(sid, @"^HX[0-9a-fA-F]{32}$"))
            return Failure("Falta un ContentSid HX válido para el tipo solicitado (los otros tipos están deshabilitados por defecto).");
        if (!string.IsNullOrWhiteSpace(n.ContentSid) && n.ContentSid != sid)
            return Failure("La plantilla configurada cambió desde el encolado; revisar el registro antes de enviar.");
        if (n.VariablesPlantilla.Count == 0 ||
            (n.Tipo == TipoNotificacionWhatsApp.PorVencer &&
             (n.VariablesPlantilla.Count != 2 || !n.VariablesPlantilla.ContainsKey("1") || !n.VariablesPlantilla.ContainsKey("2"))))
            return Failure("Faltan variables estructuradas nombre/fecha. Revisión manual requerida para registros antiguos.");

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"2010-04-01/Accounts/{Uri.EscapeDataString(_options.AccountSid)}/Messages.json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.AccountSid}:{_options.AuthToken}")));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = $"whatsapp:{n.Telefono}", ["From"] = $"whatsapp:{_options.WhatsAppFromNumber}",
            ["ContentSid"] = sid, ["ContentVariables"] = JsonSerializer.Serialize(n.VariablesPlantilla)
        });
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var status = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            int? code = null;
            string? messageSid = null;
            try
            {
                using var json = JsonDocument.Parse(body);
                if (json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("sid", out var value) && value.ValueKind == JsonValueKind.String)
                    messageSid = value.GetString();
                if (json.RootElement.ValueKind == JsonValueKind.Object && json.RootElement.TryGetProperty("code", out var errorCode) && errorCode.ValueKind == JsonValueKind.Number && errorCode.TryGetInt32(out var number)) code = number;
            }
            catch (JsonException) { /* Nunca registrar el body. */ }
            category = response.IsSuccessStatusCode ? "Accepted" : status switch
            { 401 or 403 => "Authentication", 429 => "RateLimit", >= 500 => "ProviderAmbiguous", _ => "ProviderRejected" };
            logger.LogInformation("WhatsApp HTTP {HttpStatus} Category {Category} ProviderCode {ProviderCode} CorrelationId {CorrelationId}",
                status, category, code, n.CorrelationId);
            if (response.IsSuccessStatusCode)
                return messageSid is not null && Regex.IsMatch(messageSid, @"^(SM|MM)[0-9a-fA-F]{32}$")
                    ? new(true, ProviderMessageId: messageSid)
                    : new(false, $"Respuesta aceptada sin SID válido; revisión manual. CorrelationId={n.CorrelationId}");
            return new(false, $"HTTP={status}; categoría={category}; código={code}; CorrelationId={n.CorrelationId}",
                ResultadoDefinitivo: status is >= 400 and < 500);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            category = "TransportAmbiguous";
            logger.LogWarning("WhatsApp HTTP unavailable Category {Category} CorrelationId {CorrelationId}", category, n.CorrelationId);
            return new(false, $"Resultado ambiguo; verificar en Twilio. CorrelationId={n.CorrelationId}");
        }

        WhatsAppSendResult Failure(string message)
        {
            logger.LogWarning("WhatsApp HTTP not_attempted Category {Category} CorrelationId {CorrelationId}", category, n.CorrelationId);
            return new(false, message, ResultadoDefinitivo: true);
        }
    }
    private static bool NotificacionServicePhone(string phone) => !string.IsNullOrWhiteSpace(phone) && Regex.IsMatch(phone, @"^\+[1-9]\d{7,14}$");
}
