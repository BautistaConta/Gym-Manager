using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace GymManager.API.Models;

public sealed class EventoAuditoria : IGymOwned
{
    public string GymId { get; set; } = null!;
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Tipo { get; set; } = null!;
    public string EntidadId { get; set; } = null!;
    public string? UserId { get; set; }
    public DateTime FechaUtc { get; set; }
    public string? Detalle { get; set; }
}
