using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using MongoDB.Driver;
using GymManager.API.Options;
using Microsoft.Extensions.Options;
using GymManager.API.DTOs;

namespace GymManager.API.Repositories;

public class NotificacionRepository : INotificacionRepository
{
    private readonly IMongoCollection<NotificacionWhatsApp> _collection;
    private readonly IGymContext _gymContext;

    private readonly WhatsAppOptions _whatsApp;
    private readonly TwilioOptions _twilio;

    public NotificacionRepository(MongoDbContext context, IGymContext gymContext, IOptions<WhatsAppOptions> whatsApp, IOptions<TwilioOptions> twilio)
    {
        _collection = context.NotificacionesWhatsApp;
        _gymContext = gymContext;
        _whatsApp = whatsApp.Value;
        _twilio = twilio.Value;
    }

    public async Task<NotificacionWhatsApp> CreateIfAbsentAsync(NotificacionWhatsApp notificacion)
    {
        TenantFilters.Stamp(notificacion, _gymContext.GymId);
        try { await _collection.InsertOneAsync(notificacion); return notificacion; }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return await _collection.Find(With(n => n.ClaveDeduplicacion == notificacion.ClaveDeduplicacion))
                .FirstAsync();
        }
    }

    public Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId)
    {
        var filter = TenantFilters.ForGym<NotificacionWhatsApp>(_gymContext.GymId);
        if (estado.HasValue) filter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Estado, estado.Value);
        if (!string.IsNullOrWhiteSpace(alumnoId)) filter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.AlumnoId, alumnoId);
        return _collection.Find(filter).SortByDescending(n => n.FechaCreacion).ToListAsync();
    }

    public async Task<NotificacionWhatsApp?> GetByIdAsync(string id) => await _collection.Find(ById(id)).FirstOrDefaultAsync();

    public async Task<NotificacionPageData> SearchAsync(NotificacionHistorialQuery query)
    {
        var baseFilter = TenantFilters.ForGym<NotificacionWhatsApp>(_gymContext.GymId);
        if (query.Tipo.HasValue) baseFilter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Tipo, query.Tipo.Value);
        if (!string.IsNullOrWhiteSpace(query.AlumnoId)) baseFilter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.AlumnoId, query.AlumnoId.Trim());
        if (query.DesdeUtc.HasValue) baseFilter &= Builders<NotificacionWhatsApp>.Filter.Gte(n => n.FechaCreacion, query.DesdeUtc.Value);
        if (query.HastaUtc.HasValue) baseFilter &= Builders<NotificacionWhatsApp>.Filter.Lte(n => n.FechaCreacion, query.HastaUtc.Value);
        var itemFilter = query.Estado.HasValue
            ? baseFilter & Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Estado, query.Estado.Value)
            : baseFilter;
        var skip = (query.Pagina - 1) * query.TamanoPagina;
        var itemsTask = _collection.Find(itemFilter).SortByDescending(n => n.FechaCreacion)
            .Skip(skip).Limit(query.TamanoPagina).ToListAsync();
        var totalTask = _collection.CountDocumentsAsync(itemFilter);
        var pendingTask = _collection.CountDocumentsAsync(baseFilter & Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Estado, EstadoNotificacionWhatsApp.Pendiente));
        var acceptedTask = _collection.CountDocumentsAsync(baseFilter & Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Estado, EstadoNotificacionWhatsApp.AceptadoPorTwilio));
        var failedTask = _collection.CountDocumentsAsync(baseFilter & Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Estado, EstadoNotificacionWhatsApp.Fallido));
        var reviewTask = _collection.CountDocumentsAsync(baseFilter & Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Estado, EstadoNotificacionWhatsApp.RequiereRevision));
        await Task.WhenAll(itemsTask, totalTask, pendingTask, acceptedTask, failedTask, reviewTask);
        return new(await itemsTask, await totalTask,
            new((int)await pendingTask, (int)await acceptedTask, (int)await failedTask, (int)await reviewTask));
    }

    public Task<List<NotificacionWhatsApp>> GetByCampaniaAsync(string campaniaId) =>
        _collection.Find(With(n => n.CampaniaId == campaniaId)).ToListAsync();

    public async Task<NotificacionWhatsApp?> ClaimNextAsync(DateTime nowUtc)
    {
        var filter = With(n => n.Estado == EstadoNotificacionWhatsApp.Pendiente &&
            n.Tipo != TipoNotificacionWhatsApp.PagoConfirmado && n.ClaveDeduplicacion != "");
        filter &= Builders<NotificacionWhatsApp>.Filter.Ne(n => n.EsPrueba, true);
        if (!_twilio.AdditionalTypesEnabled)
            filter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Tipo, TipoNotificacionWhatsApp.PorVencer);
        if (!_whatsApp.CampaignsEnabled)
            filter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.CampaniaId, null);
        var update = Builders<NotificacionWhatsApp>.Update
            .Set(n => n.Estado, EstadoNotificacionWhatsApp.Procesando)
            .Set(n => n.FechaInicioProcesamiento, nowUtc)
            .Set(n => n.FechaActualizacion, nowUtc)
            .Inc(n => n.Intentos, 1);
        return await _collection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<NotificacionWhatsApp>
        {
            Sort = Builders<NotificacionWhatsApp>.Sort.Ascending(n => n.FechaCreacion),
            ReturnDocument = ReturnDocument.After
        });
    }

    public async Task<bool> TransitionAsync(string id, EstadoNotificacionWhatsApp expected, EstadoNotificacionWhatsApp next,
        DateTime nowUtc, string? error = null, string? providerMessageId = null)
    {
        var filter = With(n => n.Id == id && n.Estado == expected);
        var update = Builders<NotificacionWhatsApp>.Update
            .Set(n => n.Estado, next)
            .Set(n => n.FechaActualizacion, nowUtc)
            .Set(n => n.ErrorDetalle, error)
            .Set(n => n.ProviderMessageId, providerMessageId);
        if (next == EstadoNotificacionWhatsApp.AceptadoPorTwilio) update = update.Set(n => n.FechaEnvio, nowUtc);
        if (next == EstadoNotificacionWhatsApp.Procesando) update = update.Set(n => n.FechaInicioProcesamiento, nowUtc).Inc(n => n.Intentos, 1);
        if (next == EstadoNotificacionWhatsApp.RequiereRevision) update = update.Set(n => n.FechaRevision, nowUtc);
        return (await _collection.UpdateOneAsync(filter, update)).ModifiedCount == 1;
    }

    public async Task<long> MarkProcessingForReviewAsync(DateTime nowUtc)
    {
        var filter = With(n => n.Estado == EstadoNotificacionWhatsApp.Procesando);
        var update = Builders<NotificacionWhatsApp>.Update
            .Set(n => n.Estado, EstadoNotificacionWhatsApp.RequiereRevision)
            .Set(n => n.FechaRevision, nowUtc)
            .Set(n => n.FechaActualizacion, nowUtc)
            .Set(n => n.ErrorDetalle, "El proceso se interrumpió durante un envío; verificar en Twilio antes de actuar.");
        return (await _collection.UpdateManyAsync(filter, update)).ModifiedCount;
    }

    public async Task<long> CancelPendingByCampaniaAsync(string campaniaId, DateTime nowUtc)
    {
        var filter = With(n => n.CampaniaId == campaniaId && n.Estado == EstadoNotificacionWhatsApp.Pendiente);
        var update = Builders<NotificacionWhatsApp>.Update
            .Set(n => n.Estado, EstadoNotificacionWhatsApp.Descartado)
            .Set(n => n.FechaActualizacion, nowUtc)
            .Set(n => n.ErrorDetalle, "Campaña cancelada antes del envío.");
        return (await _collection.UpdateManyAsync(filter, update)).ModifiedCount;
    }

    private FilterDefinition<NotificacionWhatsApp> ById(string id) => With(n => n.Id == id);
    private FilterDefinition<NotificacionWhatsApp> With(System.Linq.Expressions.Expression<Func<NotificacionWhatsApp, bool>> filter) =>
        TenantFilters.And<NotificacionWhatsApp>(_gymContext.GymId, Builders<NotificacionWhatsApp>.Filter.Where(filter));
}
