using GymManager.API.Models;

namespace GymManager.API.DTOs;

public sealed class NotificacionHistorialQuery
{
    public EstadoNotificacionWhatsApp? Estado { get; set; }
    public TipoNotificacionWhatsApp? Tipo { get; set; }
    public string? AlumnoId { get; set; }
    public DateTime? DesdeUtc { get; set; }
    public DateTime? HastaUtc { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
}

public sealed record NotificacionContadores(int Pendientes, int AceptadasPorTwilio, int Fallidas, int EnRevision);
public sealed record NotificacionHistorialItem(
    string Id, string AlumnoId, string Alumno, string Telefono,
    string? SucursalId, string? Sucursal, TipoNotificacionWhatsApp Tipo,
    DateTime? FechaVencimiento, DateTime FechaCreacion, DateTime? FechaEnvio,
    EstadoNotificacionWhatsApp Estado, string EstadoDescripcion, int Intentos,
    string? ProviderMessageId, string CorrelationId, string? ErrorResumen, bool PuedeReintentar);
public sealed record NotificacionHistorialResponse(
    IReadOnlyList<NotificacionHistorialItem> Items, long Total, int Pagina,
    int TamanoPagina, int TotalPaginas, NotificacionContadores Contadores);

public sealed record NotificacionPageData(List<NotificacionWhatsApp> Items, long Total, NotificacionContadores Contadores);
