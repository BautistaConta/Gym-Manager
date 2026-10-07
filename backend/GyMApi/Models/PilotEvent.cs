using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace GymManager.API.Models;

public sealed class PilotEvent : IGymOwned
{
    public string GymId { get; set; } = null!;

    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string Tipo { get; set; } = null!;
    public string? EntidadId { get; set; }
    public string? UsuarioId { get; set; }
    public string CorrelationId { get; set; } = null!;
    public DateTime FechaUtc { get; set; }
}

public static class PilotEventTypes
{
    public const string LoginExitoso = "LoginExitoso";
    public const string AlumnoCreado = "AlumnoCreado";
    public const string AlumnoActualizado = "AlumnoActualizado";
    public const string AlumnoDesactivado = "AlumnoDesactivado";
    public const string PagoRegistrado = "PagoRegistrado";
    public const string SucursalCreada = "SucursalCreada";
    public const string SucursalActualizada = "SucursalActualizada";
    public const string NotificacionAceptada = "NotificacionAceptada";
    public const string NotificacionFallida = "NotificacionFallida";
    public const string NotificacionReenviada = "NotificacionReenviada";
}
