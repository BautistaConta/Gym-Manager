using GymManager.API.Options;
using Microsoft.Extensions.Options;

namespace GymManager.API.Services;

public enum EstadoCuota { SIN_PAGOS, VENCIDA, PROXIMO_A_VENCER, AL_DIA }

public sealed record ResultadoCuota(EstadoCuota Estado, DateTime? FechaVencimiento);
public sealed record PeriodoCuota(DateTime Desde, DateTime Hasta);

public sealed class CuotaCalculator
{
    private readonly TimeZoneInfo _zone;
    private readonly int _days;
    private readonly TimeProvider _clock;

    public CuotaCalculator(IOptions<CuotasOptions> options, TimeProvider clock)
    {
        _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
        _days = options.Value.DiasProximoAVencer;
        _clock = clock;
    }

    public DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_clock.GetUtcNow(), _zone).DateTime);
    public DateTime AhoraUtc => _clock.GetUtcNow().UtcDateTime;

    // PeriodoDesde y PeriodoHasta son fechas de negocio inclusivas, serializadas como UTC 00:00.
    public ResultadoCuota Evaluar(DateTime? periodoHasta)
    {
        if (periodoHasta is null) return new(EstadoCuota.SIN_PAGOS, null);
        var fecha = DateOnly.FromDateTime(periodoHasta.Value);
        var hoy = Hoy;
        var estado = fecha < hoy ? EstadoCuota.VENCIDA
            : fecha <= hoy.AddDays(_days) ? EstadoCuota.PROXIMO_A_VENCER
            : EstadoCuota.AL_DIA;
        return new(estado, FechaUtc(fecha));
    }

    public PeriodoCuota NuevoPeriodo(DateTime? ultimoHasta, int meses, DateTime? hastaManual)
    {
        if (meses <= 0) throw new DomainException("La duración debe ser mayor a cero.");
        var hoy = Hoy;
        var anterior = ultimoHasta.HasValue ? DateOnly.FromDateTime(ultimoHasta.Value) : (DateOnly?)null;
        var desde = anterior >= hoy ? anterior.Value.AddDays(1) : hoy;
        var hasta = hastaManual.HasValue
            ? DateOnly.FromDateTime(hastaManual.Value)
            : desde.AddMonths(meses);
        if (hasta < desde) throw new DomainException("La fecha manual debe ser igual o posterior al inicio del nuevo período.");
        return new(FechaUtc(desde), FechaUtc(hasta));
    }

    private static DateTime FechaUtc(DateOnly date) => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
}
