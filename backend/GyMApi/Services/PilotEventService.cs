using System.Security.Claims;
using GymManager.API.DTOs;
using GymManager.API.Models;
using GymManager.API.Repositories;
using GymManager.API.Tenancy;

namespace GymManager.API.Services;

public interface IPilotEventRecorder
{
    Task RecordAsync(string tipo, string? entidadId = null, CancellationToken cancellationToken = default);
    Task RecordForGymAsync(string gymId, string tipo, string? entidadId = null,
        string? usuarioId = null, CancellationToken cancellationToken = default);
}

public sealed class PilotEventService(
    IPilotEventRepository repository,
    IGymContext gym,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider clock,
    ILogger<PilotEventService> logger) : IPilotEventRecorder
{
    public Task RecordAsync(string tipo, string? entidadId = null, CancellationToken cancellationToken = default)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return RecordForGymAsync(gym.GymId, tipo, entidadId, userId, cancellationToken);
    }

    public async Task RecordForGymAsync(string gymId, string tipo, string? entidadId = null,
        string? usuarioId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gymId)) throw new InvalidOperationException("No se pudo determinar el gimnasio del evento.");
        var correlationId = httpContextAccessor.HttpContext?.TraceIdentifier;
        if (string.IsNullOrWhiteSpace(correlationId)) correlationId = Guid.NewGuid().ToString("N");
        var value = new PilotEvent
        {
            GymId = gymId,
            Tipo = tipo,
            EntidadId = entidadId,
            UsuarioId = usuarioId,
            CorrelationId = correlationId,
            FechaUtc = clock.GetUtcNow().UtcDateTime
        };
        try
        {
            await repository.InsertAsync(value, cancellationToken);
            logger.LogInformation("PilotEvent {EventType} registrado para entidad {EntityId}.", tipo, entidadId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // La telemetría es best-effort y nunca debe revertir una operación de negocio exitosa.
            logger.LogWarning(exception, "No se pudo registrar PilotEvent {EventType} para entidad {EntityId}.", tipo, entidadId);
        }
    }

    public async Task<PilotEventSummaryResponse> GetSummaryAsync(
        PilotEventSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var hasta = EnsureUtc(query.HastaUtc ?? now.AddDays(1).Date);
        var desde = EnsureUtc(query.DesdeUtc ?? hasta.AddDays(-7));
        if (desde >= hasta) throw new DomainException("El rango de eventos es inválido.");
        if (hasta - desde > TimeSpan.FromDays(31))
            throw new DomainException("El resumen admite un rango máximo de 31 días.");
        var counts = await repository.SummaryAsync(gym.GymId, desde, hasta, cancellationToken);
        return new(desde, hasta, counts);
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
