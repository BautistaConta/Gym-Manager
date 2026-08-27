using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace GymManager.API.Models
{   
public class Alumno
{
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Nombre { get; set; }
    public string DNI { get; set; }
    public string Telefono { get; set; }
    public DateTime FechaAlta { get; set; }
    public bool Activo { get; set; }
}
}
