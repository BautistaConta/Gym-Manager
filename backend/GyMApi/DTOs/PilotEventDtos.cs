namespace GymManager.API.DTOs;

public sealed class PilotEventSummaryQuery
{
    public DateTime? DesdeUtc { get; set; }
    public DateTime? HastaUtc { get; set; }
}

public sealed record PilotEventCount(DateTime DiaUtc, string Tipo, long Cantidad);

public sealed record PilotEventSummaryResponse(
    DateTime DesdeUtc,
    DateTime HastaUtc,
    IReadOnlyList<PilotEventCount> Conteos);
