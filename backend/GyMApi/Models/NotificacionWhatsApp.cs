using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace GymManager.API.Models;

public class NotificacionWhatsApp
{
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string AlumnoId { get; set; } = null!;
    public TipoNotificacionWhatsApp Tipo { get; set; }
    public string Telefono { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public EstadoNotificacionWhatsApp Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string? ErrorDetalle { get; set; }
}
