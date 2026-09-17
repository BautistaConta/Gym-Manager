using GymManager.API.DTOs;
using GymManager.API.Models;
using GymManager.API.Options;
using GymManager.API.Repositories;
using Microsoft.Extensions.Options;

namespace GymManager.API.Services;

public sealed class CampaniaWhatsAppService(
    CampaniaWhatsAppRepository campanias,
    AlumnoRepository alumnos,
    SucursalRepository sucursales,
    IConfiguracionGymRepository configuraciones,
    INotificacionRepository notificaciones,
    IAuditoriaRepository auditoria,
    IOptions<TwilioOptions> twilioOptions,
    IOptions<WhatsAppOptions> whatsAppOptions,
    TimeProvider clock)
{
    private readonly TwilioOptions _twilio = twilioOptions.Value;
    private readonly WhatsAppOptions _options = whatsAppOptions.Value;

    public async Task<CampaniaWhatsApp> CrearAsync(CrearCampaniaRequest request, string userId, string creador)
    {
        if (!_options.CampaignsEnabled) throw new DomainException("Las campañas de WhatsApp están deshabilitadas.");
        if (string.IsNullOrWhiteSpace(request.NombreInterno)) throw new DomainException("El nombre interno es obligatorio.");
        ValidateVariables(request.TipoPlantilla, request.Variables);
        if (request.TipoAudiencia == TipoAudienciaCampania.SucursalPrincipal &&
            (string.IsNullOrWhiteSpace(request.SucursalPrincipalId) || await sucursales.GetByIdAsync(request.SucursalPrincipalId) is null))
            throw new DomainException("La sucursal principal no existe en el gimnasio actual.");
        if (request.TipoAudiencia == TipoAudienciaCampania.SeleccionManual && request.AlumnoIds.Count == 0)
            throw new DomainException("Seleccioná al menos un alumno.");
        var value = new CampaniaWhatsApp
        {
            NombreInterno = request.NombreInterno.Trim(), TipoPlantilla = request.TipoPlantilla,
            ContentSid = ContentSid(request.TipoPlantilla), Variables = request.Variables.ToDictionary(x => x.Key, x => x.Value.Trim()),
            TipoAudiencia = request.TipoAudiencia, SucursalPrincipalId = request.SucursalPrincipalId,
            AlumnoIds = request.AlumnoIds.Distinct().ToList(), UserIdCreador = userId, CreadorNombre = creador,
            FechaCreacion = clock.GetUtcNow().UtcDateTime
        };
        await campanias.CreateAsync(value);
        await auditoria.RegistrarAsync("CampaniaCreada", value.Id, userId);
        return value;
    }

    public async Task<List<CampaniaWhatsApp>> GetAllAsync()
    {
        var values = await campanias.GetAllAsync();
        foreach (var value in values) await RefreshCounts(value);
        return values;
    }

    public async Task<CampaniaWhatsApp> GetAsync(string id)
    {
        var value = await campanias.GetByIdAsync(id) ?? throw new DomainException("Campaña no encontrada.");
        await RefreshCounts(value);
        return value;
    }

    public async Task<PreviewCampaniaResponse> PreviewAsync(string id)
    {
        EnsureEnabled();
        var campaign = await campanias.GetByIdAsync(id) ?? throw new DomainException("Campaña no encontrada.");
        if (campaign.Estado != EstadoCampaniaWhatsApp.Borrador) throw new DomainException("Solo se puede previsualizar un borrador.");
        var resolved = await ResolveAudience(campaign);
        CampaniaAudienceResolver.EnsureWithinMaximum(resolved.Elegibles.Count, _options.MaxRecipientsPerCampaign);
        var token = CampaniaAudienceResolver.PreviewToken(resolved);
        campaign.UltimoPreviewToken = token;
        campaign.CantidadObjetivo = resolved.Elegibles.Count;
        campaign.Omitidas = resolved.Omitidos.Values.Sum();
        await campanias.UpdateAsync(campaign);
        var config = await configuraciones.GetAsync();
        var sampleName = resolved.Elegibles.FirstOrDefault()?.Nombre ?? "Alumno";
        return new(campaign.Id, campaign.TipoPlantilla.ToString(), BuildExample(campaign, sampleName, config?.NombreComercial ?? "Gimnasio"),
            AudienceLabel(campaign), resolved.Elegibles.Count, resolved.Omitidos.Values.Sum(),
            resolved.Omitidos.Select(x => new MotivoOmisionCampania(x.Key, x.Value)).ToList(), token);
    }

    public async Task<CampaniaWhatsApp> ConfirmarAsync(string id, ConfirmarCampaniaRequest request, string userId)
    {
        EnsureEnabled();
        if (!request.ConfirmacionExplicita) throw new DomainException("Confirmá explícitamente el envío y sus posibles cargos.");
        var campaign = await campanias.GetByIdAsync(id) ?? throw new DomainException("Campaña no encontrada.");
        if (campaign.Estado is not (EstadoCampaniaWhatsApp.Borrador or EstadoCampaniaWhatsApp.Encolando)) return campaign;
        var resolved = await ResolveAudience(campaign);
        var token = CampaniaAudienceResolver.PreviewToken(resolved);
        if ((campaign.Estado == EstadoCampaniaWhatsApp.Borrador && request.PreviewToken != token) || request.CantidadConfirmada != resolved.Elegibles.Count)
            throw new DomainException("La audiencia cambió desde la vista previa. Volvé a previsualizar y confirmar.");
        CampaniaAudienceResolver.EnsureWithinMaximum(resolved.Elegibles.Count, _options.MaxRecipientsPerCampaign);
        campaign.Estado = EstadoCampaniaWhatsApp.Encolando;
        campaign.FechaConfirmacion ??= clock.GetUtcNow().UtcDateTime;
        campaign.CantidadObjetivo = resolved.Elegibles.Count;
        campaign.Omitidas = resolved.Omitidos.Values.Sum();
        await campanias.UpdateAsync(campaign);
        var config = await configuraciones.GetAsync();
        foreach (var alumno in resolved.Elegibles)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var type = campaign.TipoPlantilla == TipoPlantillaCampania.Promocion
                ? TipoNotificacionWhatsApp.Promocion : TipoNotificacionWhatsApp.AvisoGeneral;
            await notificaciones.CreateIfAbsentAsync(new NotificacionWhatsApp
            {
                GymId = alumno.GymId, AlumnoId = alumno.Id, CampaniaId = campaign.Id,
                SucursalId = alumno.SucursalPrincipalId, ClaveDeduplicacion = $"{alumno.GymId}:campania:{campaign.Id}:{alumno.Id}",
                Tipo = type, Telefono = alumno.Telefono, ContentSid = campaign.ContentSid,
                VariablesPlantilla = BuildVariables(campaign, alumno.Nombre, config?.NombreComercial ?? "Gimnasio"),
                Mensaje = BuildExample(campaign, alumno.Nombre, config?.NombreComercial ?? "Gimnasio"),
                Estado = EstadoNotificacionWhatsApp.Pendiente, FechaCreacion = now, FechaActualizacion = now
            });
        }
        campaign.Estado = resolved.Elegibles.Count == 0 ? EstadoCampaniaWhatsApp.Finalizada : EstadoCampaniaWhatsApp.EnProceso;
        campaign.Pendientes = resolved.Elegibles.Count;
        if (resolved.Elegibles.Count == 0) campaign.FechaFinalizacion = clock.GetUtcNow().UtcDateTime;
        await campanias.UpdateAsync(campaign);
        await auditoria.RegistrarAsync("CampaniaConfirmada", campaign.Id, userId, $"Destinatarios: {resolved.Elegibles.Count}");
        return campaign;
    }

    public async Task<CampaniaWhatsApp> CancelarAsync(string id, string userId)
    {
        var campaign = await campanias.GetByIdAsync(id) ?? throw new DomainException("Campaña no encontrada.");
        if (campaign.Estado is EstadoCampaniaWhatsApp.Finalizada or EstadoCampaniaWhatsApp.Cancelada)
            throw new DomainException("La campaña ya terminó.");
        await notificaciones.CancelPendingByCampaniaAsync(id, clock.GetUtcNow().UtcDateTime);
        campaign.Estado = EstadoCampaniaWhatsApp.Cancelada;
        campaign.FechaFinalizacion = clock.GetUtcNow().UtcDateTime;
        await RefreshCounts(campaign);
        await auditoria.RegistrarAsync("CampaniaCancelada", id, userId);
        return campaign;
    }

    public async Task<int> PreviewReintentoAsync(string id)
    {
        var campaign = await campanias.GetByIdAsync(id) ?? throw new DomainException("Campaña no encontrada.");
        if (campaign.Estado == EstadoCampaniaWhatsApp.Cancelada) return 0;
        return (await notificaciones.GetByCampaniaAsync(id)).Count(n => n.Estado == EstadoNotificacionWhatsApp.Fallido);
    }

    public async Task<int> ReintentarFallidasAsync(string id, ReintentarCampaniaRequest request)
    {
        EnsureEnabled();
        if (!request.ConfirmacionExplicita) throw new DomainException("Confirmá explícitamente el reintento y sus posibles cargos.");
        var campaign = await campanias.GetByIdAsync(id) ?? throw new DomainException("Campaña no encontrada.");
        if (campaign.Estado == EstadoCampaniaWhatsApp.Cancelada) throw new DomainException("La campaña está cancelada.");
        var failed = (await notificaciones.GetByCampaniaAsync(id)).Where(n => n.Estado == EstadoNotificacionWhatsApp.Fallido).ToList();
        if (failed.Count != request.CantidadConfirmada)
            throw new DomainException("La cantidad de fallos cambió. Volvé a consultar antes de confirmar.");
        var count = 0;
        foreach (var item in failed)
            if (await notificaciones.TransitionAsync(item.Id, EstadoNotificacionWhatsApp.Fallido,
                EstadoNotificacionWhatsApp.Pendiente, clock.GetUtcNow().UtcDateTime)) count++;
        campaign.Estado = count > 0 ? EstadoCampaniaWhatsApp.EnProceso : campaign.Estado;
        await campanias.UpdateAsync(campaign);
        return count;
    }

    private async Task RefreshCounts(CampaniaWhatsApp campaign)
    {
        if (campaign.Estado is EstadoCampaniaWhatsApp.Borrador or EstadoCampaniaWhatsApp.Encolando) return;
        var items = await notificaciones.GetByCampaniaAsync(campaign.Id);
        campaign.Pendientes = items.Count(n => n.Estado is EstadoNotificacionWhatsApp.Pendiente or EstadoNotificacionWhatsApp.Procesando);
        campaign.AceptadasPorTwilio = items.Count(n => n.Estado == EstadoNotificacionWhatsApp.Enviado);
        campaign.Fallidas = items.Count(n => n.Estado is EstadoNotificacionWhatsApp.Fallido or EstadoNotificacionWhatsApp.RequiereRevision);
        if (campaign.Estado != EstadoCampaniaWhatsApp.Cancelada && campaign.Pendientes == 0)
        {
            campaign.Estado = campaign.Fallidas > 0 ? EstadoCampaniaWhatsApp.Parcial : EstadoCampaniaWhatsApp.Finalizada;
            campaign.FechaFinalizacion ??= clock.GetUtcNow().UtcDateTime;
        }
        await campanias.UpdateAsync(campaign);
    }

    private async Task<AudienciaCampaniaResuelta> ResolveAudience(CampaniaWhatsApp c) =>
        CampaniaAudienceResolver.Resolve(await alumnos.GetAllAsync(), c);

    private string ContentSid(TipoPlantillaCampania type)
    {
        var sid = type == TipoPlantillaCampania.Promocion ? _twilio.PromotionContentSid : _twilio.GeneralNoticeContentSid;
        if (string.IsNullOrWhiteSpace(sid)) throw new DomainException($"Falta configurar el ContentSid de {type}.");
        return sid;
    }
    private void EnsureEnabled()
    {
        if (!_options.CampaignsEnabled) throw new DomainException("Las campañas de WhatsApp están deshabilitadas.");
    }
    private static void ValidateVariables(TipoPlantillaCampania type, Dictionary<string, string> values)
    {
        var allowed = type == TipoPlantillaCampania.Promocion ? new[] { "titulo", "detalle" } : new[] { "asunto", "detalle" };
        if (values.Count != allowed.Length || allowed.Any(k => !values.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)) || values.Keys.Except(allowed).Any())
            throw new DomainException($"La plantilla {type} requiere únicamente: {string.Join(", ", allowed)}.");
        if (values.Values.Any(v => v.Length > 300)) throw new DomainException("Las variables no pueden superar 300 caracteres.");
    }
    private static string Subject(CampaniaWhatsApp c) => c.Variables[c.TipoPlantilla == TipoPlantillaCampania.Promocion ? "titulo" : "asunto"];
    private static Dictionary<string, string> BuildVariables(CampaniaWhatsApp c, string alumno, string gym) => new()
    {
        ["1"] = alumno, ["2"] = gym, ["3"] = Subject(c), ["4"] = c.Variables["detalle"]
    };
    private static string BuildExample(CampaniaWhatsApp c, string alumno, string gym) =>
        $"Hola {alumno}, {gym}: {Subject(c)}. {c.Variables["detalle"]}";
    private static string AudienceLabel(CampaniaWhatsApp c) => c.TipoAudiencia switch
    {
        TipoAudienciaCampania.SucursalPrincipal => $"Sucursal principal {c.SucursalPrincipalId}",
        TipoAudienciaCampania.SeleccionManual => $"Selección manual ({c.AlumnoIds.Count})",
        _ => "Todos los alumnos activos con consentimiento"
    };
}
