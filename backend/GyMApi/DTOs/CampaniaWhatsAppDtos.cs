using GymManager.API.Models;

namespace GymManager.API.DTOs;

public sealed class CrearCampaniaRequest
{
    public string NombreInterno { get; set; } = null!;
    public TipoPlantillaCampania TipoPlantilla { get; set; }
    public Dictionary<string, string> Variables { get; set; } = [];
    public TipoAudienciaCampania TipoAudiencia { get; set; }
    public string? SucursalPrincipalId { get; set; }
    public List<string> AlumnoIds { get; set; } = [];
}

public sealed record MotivoOmisionCampania(string Motivo, int Cantidad);
public sealed record PreviewCampaniaResponse(string CampaniaId, string Plantilla, string ContenidoEjemplo,
    string Audiencia, int CantidadDestinatarios, int CantidadOmitidos,
    List<MotivoOmisionCampania> MotivosOmision, string PreviewToken);
public sealed class ConfirmarCampaniaRequest
{
    public string PreviewToken { get; set; } = null!;
    public int CantidadConfirmada { get; set; }
    public bool ConfirmacionExplicita { get; set; }
}
public sealed class ReintentarCampaniaRequest
{
    public int CantidadConfirmada { get; set; }
    public bool ConfirmacionExplicita { get; set; }
}
