using GymManager.API.Models;
using GymManager.API.Services;

namespace GyMApi.Tests.Notifications;

public class CampaniaAudienceResolverTests
{
    [Fact]
    public void Excludes_inactive_invalid_phone_and_without_consent()
    {
        var result = CampaniaAudienceResolver.Resolve(new[]
        {
            Student("ok", true, true, "+5491123456789"),
            Student("inactive", false, true, "+5491123456789"),
            Student("no-consent", true, false, "+5491123456789"),
            Student("invalid", true, true, "11 2345-6789")
        }, new CampaniaWhatsApp { TipoAudiencia = TipoAudienciaCampania.Todos });

        Assert.Equal("ok", Assert.Single(result.Elegibles).Id);
        Assert.Equal(3, result.Omitidos.Values.Sum());
        Assert.Equal(1, result.Omitidos["Alumno inactivo"]);
        Assert.Equal(1, result.Omitidos["Sin consentimiento"]);
        Assert.Equal(1, result.Omitidos["Teléfono inválido"]);
    }

    [Fact]
    public void Branch_and_manual_audiences_select_only_requested_students()
    {
        var alumnos = new[] { Student("a", true, true, "+5491123456789", "s1"), Student("b", true, true, "+5491123456790", "s2") };
        var branch = CampaniaAudienceResolver.Resolve(alumnos, new CampaniaWhatsApp
            { TipoAudiencia = TipoAudienciaCampania.SucursalPrincipal, SucursalPrincipalId = "s1" });
        var manual = CampaniaAudienceResolver.Resolve(alumnos, new CampaniaWhatsApp
            { TipoAudiencia = TipoAudienciaCampania.SeleccionManual, AlumnoIds = ["b"] });
        Assert.Equal("a", Assert.Single(branch.Elegibles).Id);
        Assert.Equal("b", Assert.Single(manual.Elegibles).Id);
    }

    [Fact]
    public void Preview_token_detects_audience_change_and_maximum_is_enforced()
    {
        var campaign = new CampaniaWhatsApp { TipoAudiencia = TipoAudienciaCampania.Todos };
        var one = CampaniaAudienceResolver.Resolve(new[] { Student("a", true, true, "+5491123456789") }, campaign);
        var two = CampaniaAudienceResolver.Resolve(new[] { Student("a", true, true, "+5491123456789"), Student("b", true, true, "+5491123456790") }, campaign);
        Assert.NotEqual(CampaniaAudienceResolver.PreviewToken(one), CampaniaAudienceResolver.PreviewToken(two));
        CampaniaAudienceResolver.EnsureWithinMaximum(1, 1);
        Assert.Throws<DomainException>(() => CampaniaAudienceResolver.EnsureWithinMaximum(2, 1));
    }

    private static Alumno Student(string id, bool active, bool consent, string phone, string? branch = null) => new()
    {
        GymId = "gym-a", Id = id, Nombre = id, DNI = id, Telefono = phone, Activo = active,
        NotificacionesHabilitadas = consent, FechaConsentimientoWhatsApp = consent ? DateTime.UtcNow : null,
        SucursalPrincipalId = branch
    };
}
