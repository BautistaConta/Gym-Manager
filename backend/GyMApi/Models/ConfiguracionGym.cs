using MongoDB.Bson.Serialization.Attributes;

namespace GymManager.API.Models;

public sealed class ConfiguracionGym : IGymOwned
{
    [BsonId]
    public string GymId { get; set; } = null!;
    public string NombreComercial { get; set; } = "Gym Manager";
    public string? WhatsAppGroupInviteUrl { get; set; }
    public string? TextoInicialChat { get; set; }
    public DateTime FechaActualizacionUtc { get; set; }
}
