using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace GymManager.API.Models;

public class NotificacionWhatsApp : IGymOwned
{
    public string GymId { get; set; } = null!;
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string AlumnoId { get; set; } = null!;
    public string PagoId { get; set; } = null!;
    public string SucursalId { get; set; } = null!;
    public DateTime FechaVencimiento { get; set; }
    public string ClaveDeduplicacion { get; set; } = null!;
    public TipoNotificacionWhatsApp Tipo { get; set; }
    public string Telefono { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public EstadoNotificacionWhatsApp Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public DateTime? FechaInicioProcesamiento { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateTime? FechaRevision { get; set; }
    public int Intentos { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorDetalle { get; set; }
}
