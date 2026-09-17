using GymManager.API.Models;
using GymManager.API.Options;
using GymManager.API.Repositories;
using GymManager.API.Services;
using Microsoft.Extensions.Options;

namespace GyMApi.Tests.Notifications;

public class BienvenidaWhatsAppServiceTests
{
    [Fact]
    public async Task Consent_and_config_enqueue_exactly_one_welcome()
    {
        var repo = new FakeNotifications();
        var service = Create(repo, new ConfiguracionGym { GymId = "gym-a", NombreComercial = "Gym A",
            WhatsAppGroupInviteUrl = "https://chat.whatsapp.com/abcdefghijk" });
        var alumno = Student(consent: true);

        var first = await service.EncolarAsync(alumno);
        var second = await service.EncolarAsync(alumno);

        Assert.Equal("Encolada", first.Estado);
        Assert.Equal("Encolada", second.Estado);
        Assert.Single(repo.Items);
        Assert.Equal(TipoNotificacionWhatsApp.Bienvenida, repo.Items[0].Tipo);
        Assert.Equal("gym-a:bienvenida:alumno-1", repo.Items[0].ClaveDeduplicacion);
    }

    [Fact]
    public async Task No_consent_or_missing_group_link_omits_welcome()
    {
        var repo = new FakeNotifications();
        var withConfig = Create(repo, new ConfiguracionGym { GymId = "gym-a", NombreComercial = "Gym A",
            WhatsAppGroupInviteUrl = "https://chat.whatsapp.com/abcdefghijk" });
        var withoutConfig = Create(repo, null);

        Assert.Equal("Omitida", (await withConfig.EncolarAsync(Student(consent: false))).Estado);
        var missing = await withoutConfig.EncolarAsync(Student(consent: true));
        Assert.Equal("Omitida", missing.Estado);
        Assert.Contains("enlace", missing.Motivo, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repo.Items);
    }

    private static BienvenidaWhatsAppService Create(FakeNotifications repo, ConfiguracionGym? config) => new(
        repo, new FakeConfig(config), new FakeAudit(), Options.Create(new TwilioOptions { WelcomeContentSid = "HXwelcome" }),
        new FixedClock(new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero)));

    private static Alumno Student(bool consent) => new() { GymId = "gym-a", Id = "alumno-1", Nombre = "Ana",
        DNI = "1", Telefono = "+5491123456789", Activo = true, NotificacionesHabilitadas = consent,
        FechaConsentimientoWhatsApp = consent ? DateTime.UtcNow : null };

    private sealed class FakeConfig(ConfiguracionGym? value) : IConfiguracionGymRepository
    {
        public Task<ConfiguracionGym?> GetAsync() => Task.FromResult(value);
        public Task UpsertAsync(ConfiguracionGym item) => Task.CompletedTask;
    }
    private sealed class FakeAudit : IAuditoriaRepository
    {
        public Task RegistrarAsync(string tipo, string entidadId, string? userId = null, string? detalle = null) => Task.CompletedTask;
    }
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
    private sealed class FakeNotifications : INotificacionRepository
    {
        public List<NotificacionWhatsApp> Items { get; } = [];
        public Task<NotificacionWhatsApp> CreateIfAbsentAsync(NotificacionWhatsApp item)
        {
            var existing = Items.FirstOrDefault(x => x.GymId == item.GymId && x.ClaveDeduplicacion == item.ClaveDeduplicacion);
            if (existing is not null) return Task.FromResult(existing);
            item.Id = Guid.NewGuid().ToString(); Items.Add(item); return Task.FromResult(item);
        }
        public Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId) => Task.FromResult(Items.ToList());
        public Task<NotificacionWhatsApp?> GetByIdAsync(string id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<List<NotificacionWhatsApp>> GetByCampaniaAsync(string campaniaId) => Task.FromResult(new List<NotificacionWhatsApp>());
        public Task<NotificacionWhatsApp?> ClaimNextAsync(DateTime nowUtc) => Task.FromResult<NotificacionWhatsApp?>(null);
        public Task<bool> TransitionAsync(string id, EstadoNotificacionWhatsApp expected, EstadoNotificacionWhatsApp next, DateTime nowUtc, string? error = null, string? providerMessageId = null) => Task.FromResult(false);
        public Task<long> MarkProcessingForReviewAsync(DateTime nowUtc) => Task.FromResult(0L);
        public Task<long> CancelPendingByCampaniaAsync(string campaniaId, DateTime nowUtc) => Task.FromResult(0L);
    }
}
