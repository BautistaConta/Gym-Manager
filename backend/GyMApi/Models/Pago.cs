using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace GymManager.API.Models
{
public class Pago
{
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string AlumnoId { get; set; } = null!;
    public string SucursalId { get; set; } = null!;
    public string CategoriaPagoId { get; set; } = null!;
    public decimal PrecioCategoria { get; set; }
    public decimal MontoFinal { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public DateTime FechaPago { get; set; }
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
}
}
