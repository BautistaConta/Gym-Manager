using GymManager.API.Models;
using GymManager.API.Options;
using GymManager.API.Repositories;
using GymManager.API.Senders;
using GymManager.API.Services;
using Microsoft.Extensions.Options;

namespace GyMApi.Tests.Notifications;

public class NotificacionServiceTests
{
    private static readonly DateTime Today = new(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PorVencer_enters_window_once_per_payment()
    {
        var h = Harness();
        h.Pago.PeriodoHasta = Today.AddDays(6);
        Assert.Null(await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Datos.UltimoPago, TipoNotificacionWhatsApp.PorVencer));

        h.Pago.PeriodoHasta = Today.AddDays(5);
        var first = await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Datos.UltimoPago, TipoNotificacionWhatsApp.PorVencer);
        var second = await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Datos.UltimoPago, TipoNotificacionWhatsApp.PorVencer);

        Assert.NotNull(first);
        Assert.Same(first, second);
        Assert.Single(h.Repo.Items);
        Assert.Equal(h.Pago.Id, first.PagoId);
        Assert.Equal(h.Pago.SucursalId, first.SucursalId);
        Assert.Equal(Today.AddDays(5), first.FechaVencimiento);
    }

    [Fact]
    public async Task Vencido_only_after_expiration_and_once()
    {
        var h = Harness();
        h.Pago.PeriodoHasta = Today;
        Assert.Null(await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Datos.UltimoPago, TipoNotificacionWhatsApp.Vencido));
        h.Pago.PeriodoHasta = Today.AddDays(-1);
        await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Datos.UltimoPago, TipoNotificacionWhatsApp.Vencido);
        await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Datos.UltimoPago, TipoNotificacionWhatsApp.Vencido);
        Assert.Single(h.Repo.Items);
    }

    [Fact]
    public async Task No_consent_inactive_or_invalid_phone_never_enqueues()
    {
        var h = Harness();
        h.Alumno.NotificacionesHabilitadas = false;
        Assert.Null(await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Pago, TipoNotificacionWhatsApp.PorVencer));
        h.Alumno.NotificacionesHabilitadas = true;
        h.Alumno.FechaConsentimientoWhatsApp = null;
        Assert.Null(await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Pago, TipoNotificacionWhatsApp.PorVencer));
        h.Alumno.FechaConsentimientoWhatsApp = Today.AddDays(-10);
        h.Alumno.Activo = false;
        Assert.Null(await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Pago, TipoNotificacionWhatsApp.PorVencer));
        h.Alumno.Activo = true;
        h.Alumno.Telefono = "3811234567";
        Assert.Null(await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Pago, TipoNotificacionWhatsApp.PorVencer));
        h.Alumno.Telefono = "+5493811234567";
        Assert.Null(await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Pago, TipoNotificacionWhatsApp.PagoConfirmado));
        Assert.Empty(h.Repo.Items);
        Assert.Equal(0, h.Sender.Calls);
    }

    [Fact]
    public async Task Concurrent_enqueues_and_claims_create_and_send_only_once()
    {
        var h = Harness();
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Datos.UltimoPago, TipoNotificacionWhatsApp.PorVencer)));
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => h.Service.ProcesarPendientesAsync(1)));

        Assert.Single(h.Repo.Items);
        Assert.Equal(1, h.Sender.Calls);
        Assert.Equal(1, h.Repo.Items[0].Intentos);
        Assert.Equal(EstadoNotificacionWhatsApp.Enviado, h.Repo.Items[0].Estado);
        Assert.Equal("SM-test", h.Repo.Items[0].ProviderMessageId);
    }

    [Fact]
    public async Task Ambiguous_send_and_interrupted_processing_never_retry_automatically()
    {
        var h = Harness();
        h.Sender.Result = new(false, "Timeout ambiguo");
        await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Datos.UltimoPago, TipoNotificacionWhatsApp.PorVencer);
        await h.Service.ProcesarPendientesAsync(1);
        await h.Service.ProcesarPendientesAsync(1);
        Assert.Equal(1, h.Sender.Calls);
        Assert.Equal(EstadoNotificacionWhatsApp.RequiereRevision, h.Repo.Items[0].Estado);
        await Assert.ThrowsAsync<DomainException>(() => h.Service.ReenviarAsync(h.Repo.Items[0].Id));

        var second = new Pago { Id = "pago-2", GymId = h.Pago.GymId,
            AlumnoId = h.Alumno.Id, SucursalId = h.Pago.SucursalId, PeriodoHasta = Today.AddDays(-1) };
        h.Datos.UltimoPago = second;
        await h.Service.EncolarSiCorrespondeAsync(h.Alumno, second, TipoNotificacionWhatsApp.Vencido);
        await h.Repo.ClaimNextAsync(Today);
        Assert.Equal(1, await h.Service.RevisarProcesandoAlIniciarAsync());
        Assert.Equal(EstadoNotificacionWhatsApp.RequiereRevision, h.Repo.Items[1].Estado);
    }

    [Fact]
    public async Task Definite_rejection_requires_explicit_retry_of_same_record()
    {
        var h = Harness();
        h.Sender.Result = new(false, "HTTP 429", ResultadoDefinitivo: true);
        await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Pago, TipoNotificacionWhatsApp.PorVencer);
        await h.Service.ProcesarPendientesAsync(1);
        Assert.Equal(EstadoNotificacionWhatsApp.Fallido, h.Repo.Items[0].Estado);
        await h.Service.ProcesarPendientesAsync(1);
        Assert.Equal(1, h.Sender.Calls);

        await h.Service.ReenviarAsync(h.Repo.Items[0].Id);
        h.Sender.Result = new(true, ProviderMessageId: "SM-retry");
        await h.Service.ProcesarPendientesAsync(1);
        Assert.Single(h.Repo.Items);
        Assert.Equal(2, h.Repo.Items[0].Intentos);
        Assert.Equal(EstadoNotificacionWhatsApp.Enviado, h.Repo.Items[0].Estado);
    }

    [Fact]
    public async Task Historical_manual_payment_is_rejected()
    {
        var h = Harness();
        h.Datos.UltimoPago = new Pago { Id = "nuevo", GymId = h.Pago.GymId,
            AlumnoId = h.Alumno.Id, SucursalId = h.Pago.SucursalId, PeriodoHasta = Today.AddDays(2) };
        await Assert.ThrowsAsync<DomainException>(() => h.Service.EncolarRecordatorioManualAsync(h.Alumno, h.Pago));
        Assert.Empty(h.Repo.Items);
    }

    [Fact]
    public async Task Revoked_consent_after_enqueue_discards_without_sending()
    {
        var h = Harness();
        await h.Service.EncolarSiCorrespondeAsync(h.Alumno, h.Datos.UltimoPago, TipoNotificacionWhatsApp.PorVencer);
        h.Alumno.NotificacionesHabilitadas = false;
        await h.Service.ProcesarPendientesAsync(1);
        Assert.Equal(EstadoNotificacionWhatsApp.Descartado, h.Repo.Items[0].Estado);
        Assert.Equal(0, h.Sender.Calls);
    }

    private static TestHarness Harness()
    {
        var alumno = new Alumno
        {
            Id = "alumno-1", GymId = "gym-1", Nombre = "Ana", Telefono = "+5493811234567",
            Activo = true, NotificacionesHabilitadas = true,
            FechaConsentimientoWhatsApp = Today.AddDays(-10)
        };
        var pago = new Pago
        {
            Id = "pago-1", GymId = "gym-1", AlumnoId = alumno.Id, SucursalId = "sede-1",
            PeriodoHasta = Today.AddDays(2)
        };
        var repo = new FakeRepo();
        var datos = new FakeDatos { Alumno = alumno, UltimoPago = pago };
        var sender = new FakeSender();
        var clock = new FixedClock(new DateTimeOffset(2026, 9, 14, 15, 0, 0, TimeSpan.Zero));
        var cuotas = new CuotaCalculator(Options.Create(new CuotasOptions()), clock);
        return new(new NotificacionService(repo, datos, sender, cuotas, clock), repo, datos, sender, alumno, pago);
    }

    private sealed record TestHarness(NotificacionService Service, FakeRepo Repo, FakeDatos Datos,
        FakeSender Sender, Alumno Alumno, Pago Pago);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeDatos : INotificacionDatos
    {
        public Alumno Alumno { get; set; } = null!;
        public Pago UltimoPago { get; set; } = null!;
        public Task<Alumno?> GetAlumnoAsync(string id) => Task.FromResult<Alumno?>(Alumno.Id == id ? Alumno : null);
        public Task<Pago?> GetUltimoPagoAsync(string alumnoId) => Task.FromResult<Pago?>(UltimoPago.AlumnoId == alumnoId ? UltimoPago : null);
    }

    private sealed class FakeSender : IWhatsAppSender
    {
        private int _calls;
        public int Calls => _calls;
        public WhatsAppSendResult Result { get; set; } = new(true, ProviderMessageId: "SM-test");
        public Task<WhatsAppSendResult> SendAsync(NotificacionWhatsApp notificacion,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeRepo : INotificacionRepository
    {
        private readonly object _gate = new();
        private readonly List<NotificacionWhatsApp> _items = [];
        public List<NotificacionWhatsApp> Items { get { lock (_gate) return _items.ToList(); } }
        public Task<NotificacionWhatsApp> CreateIfAbsentAsync(NotificacionWhatsApp n)
        {
            lock (_gate)
            {
                var existing = _items.FirstOrDefault(x => x.GymId == n.GymId && x.ClaveDeduplicacion == n.ClaveDeduplicacion);
                if (existing is not null) return Task.FromResult(existing);
                n.Id = Guid.NewGuid().ToString();
                _items.Add(n);
                return Task.FromResult(n);
            }
        }
        public Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId)
        {
            lock (_gate) return Task.FromResult(_items.Where(x => (!estado.HasValue || x.Estado == estado) &&
                (alumnoId is null || x.AlumnoId == alumnoId)).ToList());
        }
        public Task<NotificacionWhatsApp?> GetByIdAsync(string id)
        {
            lock (_gate) return Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        }
        public Task<List<NotificacionWhatsApp>> GetByCampaniaAsync(string campaniaId)
        {
            lock (_gate) return Task.FromResult(_items.Where(x => x.CampaniaId == campaniaId).ToList());
        }
        public Task<long> CancelPendingByCampaniaAsync(string campaniaId, DateTime nowUtc)
        {
            lock (_gate)
            {
                var pending = _items.Where(x => x.CampaniaId == campaniaId && x.Estado == EstadoNotificacionWhatsApp.Pendiente).ToList();
                foreach (var n in pending) { n.Estado = EstadoNotificacionWhatsApp.Descartado; n.FechaActualizacion = nowUtc; }
                return Task.FromResult((long)pending.Count);
            }
        }
        public Task<NotificacionWhatsApp?> ClaimNextAsync(DateTime nowUtc)
        {
            lock (_gate)
            {
                var n = _items.FirstOrDefault(x => x.Estado == EstadoNotificacionWhatsApp.Pendiente);
                if (n is not null)
                {
                    n.Estado = EstadoNotificacionWhatsApp.Procesando;
                    n.FechaInicioProcesamiento = nowUtc;
                    n.Intentos++;
                }
                return Task.FromResult(n);
            }
        }
        public Task<bool> TransitionAsync(string id, EstadoNotificacionWhatsApp expected, EstadoNotificacionWhatsApp next,
            DateTime nowUtc, string? error = null, string? providerMessageId = null)
        {
            lock (_gate)
            {
                var n = _items.FirstOrDefault(x => x.Id == id && x.Estado == expected);
                if (n is null) return Task.FromResult(false);
                n.Estado = next;
                n.ErrorDetalle = error;
                n.ProviderMessageId = providerMessageId;
                n.FechaActualizacion = nowUtc;
                if (next == EstadoNotificacionWhatsApp.Enviado) n.FechaEnvio = nowUtc;
                if (next == EstadoNotificacionWhatsApp.RequiereRevision) n.FechaRevision = nowUtc;
                return Task.FromResult(true);
            }
        }
        public Task<long> MarkProcessingForReviewAsync(DateTime nowUtc)
        {
            lock (_gate)
            {
                var processing = _items.Where(x => x.Estado == EstadoNotificacionWhatsApp.Procesando).ToList();
                foreach (var n in processing) { n.Estado = EstadoNotificacionWhatsApp.RequiereRevision; n.FechaRevision = nowUtc; }
                return Task.FromResult((long)processing.Count);
            }
        }
    }
}
