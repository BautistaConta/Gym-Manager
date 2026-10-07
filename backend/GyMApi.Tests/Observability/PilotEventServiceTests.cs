using System.Security.Claims;
using GymManager.API.DTOs;
using GymManager.API.Models;
using GymManager.API.Repositories;
using GymManager.API.Services;
using GymManager.API.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GyMApi.Tests.Observability;

public sealed class PilotEventServiceTests
{
    [Fact]
    public async Task Records_minimal_tenant_scoped_event_with_correlation_and_user()
    {
        var repository = new FakeRepository();
        var context = new DefaultHttpContext { TraceIdentifier = "corr-123" };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"));
        var service = new PilotEventService(repository, new TestGym(),
            new HttpContextAccessor { HttpContext = context },
            new FixedClock(), NullLogger<PilotEventService>.Instance);

        await service.RecordAsync(PilotEventTypes.PagoRegistrado, "pago-1");

        var value = Assert.Single(repository.Values);
        Assert.Equal("gym-a", value.GymId);
        Assert.Equal(PilotEventTypes.PagoRegistrado, value.Tipo);
        Assert.Equal("pago-1", value.EntidadId);
        Assert.Equal("user-1", value.UsuarioId);
        Assert.Equal("corr-123", value.CorrelationId);
        Assert.DoesNotContain(typeof(PilotEvent).GetProperties(), property =>
            property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Mensaje", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Summary_uses_current_gym_and_rejects_ranges_over_31_days()
    {
        var repository = new FakeRepository();
        var service = new PilotEventService(repository, new TestGym(),
            new HttpContextAccessor(), new FixedClock(), NullLogger<PilotEventService>.Instance);

        await service.GetSummaryAsync(new()
        {
            DesdeUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            HastaUtc = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        Assert.Equal("gym-a", repository.LastSummaryGymId);
        await Assert.ThrowsAsync<DomainException>(() => service.GetSummaryAsync(new()
        {
            DesdeUtc = new DateTime(2026, 1, 1),
            HastaUtc = new DateTime(2026, 3, 1)
        }));
    }

    private sealed class TestGym : IGymContext { public string GymId => "gym-a"; }
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
    }
    private sealed class FakeRepository : IPilotEventRepository
    {
        public List<PilotEvent> Values { get; } = [];
        public string? LastSummaryGymId { get; private set; }
        public Task InsertAsync(PilotEvent value, CancellationToken cancellationToken = default)
        { Values.Add(value); return Task.CompletedTask; }
        public Task<List<PilotEventCount>> SummaryAsync(string gymId, DateTime desdeUtc,
            DateTime hastaUtc, CancellationToken cancellationToken = default)
        { LastSummaryGymId = gymId; return Task.FromResult(new List<PilotEventCount>()); }
    }
}
