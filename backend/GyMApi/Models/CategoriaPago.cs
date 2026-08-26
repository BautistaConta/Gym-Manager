using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GymManager.API.Models
{
public class CategoriaPago
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Nombre { get; set; } = null!; 
    // Ej: "Adulto", "Niño", "Promo 3 meses"

    public decimal Precio { get; set; }

    public int MesesDuracion { get; set; }

    public TipoAbono TipoAbono { get; set; }

    public bool Activa { get; set; }

}
}