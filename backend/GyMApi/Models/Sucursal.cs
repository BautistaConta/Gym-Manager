using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace GymManager.API.Models
{
public class Sucursal : IGymOwned
{
    public string GymId { get; set; } = null!;
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Direccion { get; set; } = null!;
    public DateTime FechaAlta { get; set; }
}
}
