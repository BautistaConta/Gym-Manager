using GymManager.API.Models;
using GymManager.API.Options;
using GymManager.API.Senders;
using GymManager.API.Services;
using GymManager.API.Tenancy;
using Microsoft.Extensions.Options;

namespace GyMApi.Tests.Notifications;

public class WhatsAppSmokeTestTests
{
    [Fact]
    public async Task Repeated_or_concurrent_smoke_test_sends_once_and_leaves_history()
    {
        var repo = new NotificacionServiceTests.FakeRepo();
        var sender = new CountingSender();
        var service = Service(repo, sender, Ready());
        await Task.WhenAll(service.EnviarAsync(TipoNotificacionWhatsApp.PorVencer, true, default),
            service.EnviarAsync(TipoNotificacionWhatsApp.PorVencer, true, default));
        Assert.Equal(1, sender.Calls);
        var record = Assert.Single(repo.Items);
        Assert.Equal("gym-a", record.GymId);
        Assert.True(record.EsPrueba);
        Assert.Equal("+5491123456789", record.Telefono);
        Assert.Equal(EstadoNotificacionWhatsApp.AceptadoPorTwilio, record.Estado);
        Assert.Equal("SM11111111111111111111111111111111", record.ProviderMessageId);
    }

    [Fact]
    public async Task Missing_readiness_confirmation_and_other_types_never_send()
    {
        var repo = new NotificacionServiceTests.FakeRepo();
        var sender = new CountingSender();
        var incomplete = Service(repo, sender, new TwilioOptions());
        Assert.Contains(incomplete.IntervencionesPendientes(), x => x.Contains("cuenta paga"));
        await Assert.ThrowsAsync<DomainException>(() => incomplete.EnviarAsync(TipoNotificacionWhatsApp.PorVencer, true, default));
        var ready = Service(repo, sender, Ready());
        await Assert.ThrowsAsync<DomainException>(() => ready.EnviarAsync(TipoNotificacionWhatsApp.Vencido, false, default));
        await Assert.ThrowsAsync<DomainException>(() => ready.EnviarAsync(TipoNotificacionWhatsApp.Vencido, true, default));
        await Assert.ThrowsAsync<DomainException>(() => ready.EnviarAsync(TipoNotificacionWhatsApp.Bienvenida, true, default));
        Assert.Empty(repo.Items);
        Assert.Equal(0, sender.Calls);
    }

    [Fact]
    public async Task Ambiguous_result_is_persisted_for_review_and_never_retried()
    {
        var repo = new NotificacionServiceTests.FakeRepo();
        var sender = new CountingSender { Result = new(false, "Timeout simulado") };
        var service = Service(repo, sender, Ready());
        await service.EnviarAsync(TipoNotificacionWhatsApp.PorVencer, true, default);
        await service.EnviarAsync(TipoNotificacionWhatsApp.PorVencer, true, default);
        Assert.Equal(1, sender.Calls);
        Assert.Equal(EstadoNotificacionWhatsApp.RequiereRevision, Assert.Single(repo.Items).Estado);
    }

    private static TwilioOptions Ready() => new()
    {
        Enabled = true, SmokeTestEnabled = true, WorkerEnabled = false, PaidAccountConfirmed = true, TemplatesApprovedConfirmed = true,
        AccountSid = "ACtest", AuthToken = "secret-test", WhatsAppFromNumber = "+14155552671", AuthorizedTestNumber = "+5491123456789",
        PorVencerContentSid = "HX11111111111111111111111111111111"
    };
    private static WhatsAppSmokeTestService Service(NotificacionServiceTests.FakeRepo repo, CountingSender sender, TwilioOptions options) =>
        new(repo, sender, Options.Create(options), new TestGym(), TimeProvider.System);
    private sealed class TestGym : IGymContext { public string GymId => "gym-a"; }
    private sealed class CountingSender : IWhatsAppSender
    {
        public int Calls;
        public WhatsAppSendResult Result { get; set; } = new(true, ProviderMessageId: "SM11111111111111111111111111111111");
        public Task<WhatsAppSendResult> SendAsync(NotificacionWhatsApp n, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref Calls); return Task.FromResult(Result); }
    }
}
