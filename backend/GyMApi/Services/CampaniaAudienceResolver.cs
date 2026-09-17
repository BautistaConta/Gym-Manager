using System.Security.Cryptography;
using System.Text;
using GymManager.API.Models;

namespace GymManager.API.Services;

public sealed record AudienciaCampaniaResuelta(List<Alumno> Elegibles, Dictionary<string, int> Omitidos);

public static class CampaniaAudienceResolver
{
    public static AudienciaCampaniaResuelta Resolve(IEnumerable<Alumno> alumnos, CampaniaWhatsApp campaign)
    {
        var selected = campaign.TipoAudiencia switch
        {
            TipoAudienciaCampania.SucursalPrincipal => alumnos.Where(a => a.SucursalPrincipalId == campaign.SucursalPrincipalId),
            TipoAudienciaCampania.SeleccionManual => alumnos.Where(a => campaign.AlumnoIds.Contains(a.Id)),
            _ => alumnos
        };
        var omitted = new Dictionary<string, int>();
        var eligible = new List<Alumno>();
        foreach (var alumno in selected.OrderBy(a => a.Id))
        {
            var reason = !alumno.Activo ? "Alumno inactivo"
                : !alumno.NotificacionesHabilitadas || alumno.FechaConsentimientoWhatsApp is null ? "Sin consentimiento"
                : !NotificacionService.PhoneIsValid(alumno.Telefono) ? "Teléfono inválido" : null;
            if (reason is null) eligible.Add(alumno);
            else omitted[reason] = omitted.GetValueOrDefault(reason) + 1;
        }
        return new(eligible, omitted);
    }

    public static string PreviewToken(AudienciaCampaniaResuelta value)
    {
        var raw = string.Join("|", value.Elegibles.Select(x => x.Id).Order()) + ":" +
                  string.Join("|", value.Omitidos.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    public static void EnsureWithinMaximum(int count, int maximum)
    {
        if (count > maximum) throw new DomainException($"La audiencia supera el máximo de {maximum} destinatarios.");
    }
}
