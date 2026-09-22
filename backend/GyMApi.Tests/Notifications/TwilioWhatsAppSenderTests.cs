using System.Net;
using GymManager.API.Models;
using GymManager.API.Options;
using GymManager.API.Senders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GyMApi.Tests.Notifications;

public class TwilioWhatsAppSenderTests
{
    private const string MessageSid = "SM11111111111111111111111111111111";
    [Fact]
    public async Task Uses_por_vencer_template_structured_variables_and_returns_message_sid()
    {
        var handler = new StubHandler(HttpStatusCode.Created, "{\"sid\":\"" + MessageSid + "\",\"status\":\"queued\"}");
        var log = new SafeLogger();
        var sender = Create(handler, log);
        var result = await sender.SendAsync(Notification(TipoNotificacionWhatsApp.PorVencer));
        Assert.True(result.Exitoso);
        Assert.Equal(MessageSid, result.ProviderMessageId);
        Assert.Contains("HX11111111111111111111111111111111", handler.Form);
        Assert.Contains("ContentVariables", handler.Form);
        Assert.Contains("Ana", Uri.UnescapeDataString(handler.Form));
        Assert.DoesNotContain("contenido-privado-completo", handler.Form);
        Assert.Contains("HTTP 201", log.Messages.Single());
        Assert.Contains("correlation-test", log.Messages.Single());
        Assert.DoesNotContain("secret-test", string.Join(" ", log.Messages));
        Assert.DoesNotContain("Ana", string.Join(" ", log.Messages));
    }

    [Theory]
    [InlineData(401, true)]
    [InlineData(429, true)]
    [InlineData(422, true)]
    [InlineData(500, false)]
    public async Task Classifies_http_failures_without_logging_provider_body(int status, bool definitive)
    {
        var handler = new StubHandler((HttpStatusCode)status, "{\"code\":20003,\"message\":\"secret-test Ana full-body\"}");
        var log = new SafeLogger();
        var result = await Create(handler, log).SendAsync(Notification(TipoNotificacionWhatsApp.PorVencer));
        Assert.False(result.Exitoso);
        Assert.Equal(definitive, result.ResultadoDefinitivo);
        Assert.Contains("20003", result.ErrorDetalle);
        Assert.DoesNotContain("secret-test", string.Join(" ", log.Messages));
        Assert.DoesNotContain("full-body", result.ErrorDetalle);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("invalid json")]
    [InlineData("[]")]
    [InlineData("{\"sid\":\"not-a-sid\"}")]
    public async Task Accepted_without_valid_sid_is_ambiguous(string body)
    {
        var result = await Create(new StubHandler(HttpStatusCode.Created, body), new SafeLogger())
            .SendAsync(Notification(TipoNotificacionWhatsApp.PorVencer));
        Assert.False(result.Exitoso);
        Assert.False(result.ResultadoDefinitivo);
    }

    [Fact]
    public async Task Unauthorized_smoke_number_and_missing_variables_do_not_make_http_requests()
    {
        var handler = new StubHandler(HttpStatusCode.Created, "{}");
        var sender = Create(handler, new SafeLogger());
        var n = Notification(TipoNotificacionWhatsApp.PorVencer);
        n.EsPrueba = true;
        n.Telefono = "+5491199999999";
        Assert.False((await sender.SendAsync(n)).Exitoso);
        n.EsPrueba = false;
        n.VariablesPlantilla.Clear();
        Assert.False((await sender.SendAsync(n)).Exitoso);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task Fake_sender_never_claims_provider_acceptance()
    {
        var result = await new FakeWhatsAppSender().SendAsync(Notification(TipoNotificacionWhatsApp.PorVencer));
        Assert.True(result.Simulado);
        Assert.False(result.Exitoso);
        Assert.Null(result.ProviderMessageId);
    }

    [Fact]
    public async Task Transport_failure_is_ambiguous_and_does_not_log_exception_details()
    {
        var log = new SafeLogger();
        var handler = new StubHandler(HttpStatusCode.Created, "{}") { FailTransport = true };
        var result = await Create(handler, log).SendAsync(Notification(TipoNotificacionWhatsApp.PorVencer));
        Assert.False(result.ResultadoDefinitivo);
        Assert.Contains("TransportAmbiguous", string.Join(" ", log.Messages));
        Assert.DoesNotContain("secret-test", string.Join(" ", log.Messages));
    }

    private static NotificacionWhatsApp Notification(TipoNotificacionWhatsApp tipo) => new()
    {
        Tipo = tipo, Telefono = "+5491123456789", CorrelationId = "correlation-test", Mensaje = "contenido-privado-completo",
        VariablesPlantilla = new() { ["1"] = "Ana", ["2"] = "30/09/2026" }
    };
    private static TwilioWhatsAppSender Create(StubHandler handler, SafeLogger logger) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://example.invalid/") }, Options.Create(new TwilioOptions
        {
            Enabled = true, WorkerEnabled = true, SmokeTestEnabled = true, PaidAccountConfirmed = true, TemplatesApprovedConfirmed = true,
            AccountSid = "ACtest", AuthToken = "secret-test", WhatsAppFromNumber = "+14155552671", AuthorizedTestNumber = "+5491123456789",
            PorVencerContentSid = "HX11111111111111111111111111111111"
        }), logger);

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string Form { get; private set; } = "";
        public bool FailTransport { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Calls++; if (FailTransport) throw new HttpRequestException("secret-test private transport details");
          Form = await request.Content!.ReadAsStringAsync(cancellationToken); return new(status) { Content = new StringContent(body) }; }
    }
    private sealed class SafeLogger : ILogger<TwilioWhatsAppSender>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
