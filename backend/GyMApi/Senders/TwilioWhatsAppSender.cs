using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using GymManager.API.Models;
using GymManager.API.Options;
using Microsoft.Extensions.Options;

namespace GymManager.API.Senders;

public class TwilioWhatsAppSender : IWhatsAppSender
{
    private static readonly Regex E164 = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);
    private readonly HttpClient _httpClient;
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioWhatsAppSender> _logger;

    public TwilioWhatsAppSender(HttpClient httpClient, IOptions<TwilioOptions> options, ILogger<TwilioWhatsAppSender> logger) { _httpClient = httpClient; _options = options.Value; _logger = logger; }

    public async Task<WhatsAppSendResult> SendAsync(string telefono, string mensaje, TipoNotificacionWhatsApp tipo, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return new(false, "El envío de Twilio está deshabilitado en la configuración.", ResultadoDefinitivo: true);
        if (!E164.IsMatch(telefono)) return new(false, "El teléfono no tiene formato E.164 válido.", ResultadoDefinitivo: true);
        if (string.IsNullOrWhiteSpace(mensaje)) return new(false, "El mensaje no puede estar vacío.", ResultadoDefinitivo: true);
        if (string.IsNullOrWhiteSpace(_options.AccountSid) || string.IsNullOrWhiteSpace(_options.AuthToken) || string.IsNullOrWhiteSpace(_options.WhatsAppFromNumber)) return new(false, "Faltan credenciales o el número remitente de Twilio.", ResultadoDefinitivo: true);
        if (string.IsNullOrWhiteSpace(_options.ContentSid)) return new(false, "Twilio requiere un ContentSid de una plantilla de WhatsApp aprobada.", ResultadoDefinitivo: true);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"2010-04-01/Accounts/{Uri.EscapeDataString(_options.AccountSid)}/Messages.json");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = $"whatsapp:{telefono}",
            ["From"] = ToWhatsAppAddress(_options.WhatsAppFromNumber),
            ["ContentSid"] = _options.ContentSid,
            ["ContentVariables"] = JsonSerializer.Serialize(new Dictionary<string, string> { ["1"] = mensaje })
        });

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    using var json = JsonDocument.Parse(body);
                    var sid = json.RootElement.GetProperty("sid").GetString();
                    return string.IsNullOrWhiteSpace(sid)
                        ? new(false, "Twilio aceptó la solicitud sin identificar el mensaje.")
                        : new(true, ProviderMessageId: sid);
                }
                catch (JsonException) { return new(false, "Twilio aceptó la solicitud, pero la respuesta no pudo leerse."); }
                catch (KeyNotFoundException) { return new(false, "Twilio aceptó la solicitud sin identificador de mensaje."); }
            }
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta;
                return new(false, $"Twilio limitó los envíos (HTTP 429){(retryAfter.HasValue ? $"; reintentar después de {retryAfter.Value.TotalMinutes:0} minutos" : string.Empty)}.", ResultadoDefinitivo: true);
            }
            _logger.LogWarning("Twilio respondió HTTP {StatusCode} al envío de WhatsApp.", (int)response.StatusCode);
            return new(false, $"Twilio devolvió HTTP {(int)response.StatusCode}.",
                ResultadoDefinitivo: (int)response.StatusCode >= 400 && (int)response.StatusCode < 500);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo conectar con Twilio.");
            return new(false, "No se pudo conectar con Twilio.");
        }
    }

    private static string ToWhatsAppAddress(string phone) => phone.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase) ? phone : $"whatsapp:{phone}";
}
