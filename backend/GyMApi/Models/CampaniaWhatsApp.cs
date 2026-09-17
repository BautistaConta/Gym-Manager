using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace GymManager.API.Models;

public enum TipoPlantillaCampania { Promocion, AvisoGeneral }
public enum EstadoCampaniaWhatsApp { Borrador, Encolando, EnProceso, Finalizada, Parcial, Cancelada }
public enum TipoAudienciaCampania { Todos, SucursalPrincipal, SeleccionManual }

public sealed class CampaniaWhatsApp : IGymOwned
{
    public string GymId { get; set; } = null!;
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string NombreInterno { get; set; } = null!;
    public TipoPlantillaCampania TipoPlantilla { get; set; }
    public string ContentSid { get; set; } = null!;
    public Dictionary<string, string> Variables { get; set; } = [];
    public EstadoCampaniaWhatsApp Estado { get; set; } = EstadoCampaniaWhatsApp.Borrador;
    public TipoAudienciaCampania TipoAudiencia { get; set; }
    public string? SucursalPrincipalId { get; set; }
    public List<string> AlumnoIds { get; set; } = [];
    public string UserIdCreador { get; set; } = null!;
    public string CreadorNombre { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaFinalizacion { get; set; }
    public string? UltimoPreviewToken { get; set; }
    public int CantidadObjetivo { get; set; }
    public int Pendientes { get; set; }
    public int AceptadasPorTwilio { get; set; }
    public int Fallidas { get; set; }
    public int Omitidas { get; set; }
}
