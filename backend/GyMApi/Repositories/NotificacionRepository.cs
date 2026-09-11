using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public class NotificacionRepository : INotificacionRepository
{
    private readonly IMongoCollection<NotificacionWhatsApp> _collection;
    private readonly IGymContext _gymContext;

    public NotificacionRepository(MongoDbContext context, IGymContext gymContext)
    {
        _collection = context.NotificacionesWhatsApp;
        _gymContext = gymContext;
    }

    public Task CreateAsync(NotificacionWhatsApp notificacion)
    {
        TenantFilters.Stamp(notificacion, _gymContext.GymId);
        return _collection.InsertOneAsync(notificacion);
    }

    public Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId)
    {
        var filter = TenantFilters.ForGym<NotificacionWhatsApp>(_gymContext.GymId);
        if (estado.HasValue) filter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Estado, estado.Value);
        if (!string.IsNullOrWhiteSpace(alumnoId)) filter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.AlumnoId, alumnoId);
        return _collection.Find(filter).SortByDescending(n => n.FechaCreacion).ToListAsync();
    }

    public async Task<NotificacionWhatsApp?> GetByIdAsync(string id) => await _collection.Find(ById(id)).FirstOrDefaultAsync();

    public Task<bool> ExistsSinceAsync(string alumnoId, TipoNotificacionWhatsApp tipo, DateTime desde) =>
        _collection.Find(With(n => n.AlumnoId == alumnoId && n.Tipo == tipo && n.FechaCreacion >= desde)).AnyAsync();

    public Task<bool> ExistsEnviadaDesdeAsync(string alumnoId, TipoNotificacionWhatsApp tipo, DateTime desde) =>
        _collection.Find(With(n => n.AlumnoId == alumnoId && n.Tipo == tipo && n.Estado == EstadoNotificacionWhatsApp.Enviado && n.FechaCreacion >= desde)).AnyAsync();

    public Task UpdateAsync(NotificacionWhatsApp notificacion)
    {
        TenantFilters.EnsureOwned(notificacion, _gymContext.GymId);
        return _collection.ReplaceOneAsync(ById(notificacion.Id), notificacion);
    }

    private FilterDefinition<NotificacionWhatsApp> ById(string id) => With(n => n.Id == id);
    private FilterDefinition<NotificacionWhatsApp> With(System.Linq.Expressions.Expression<Func<NotificacionWhatsApp, bool>> filter) =>
        TenantFilters.And<NotificacionWhatsApp>(_gymContext.GymId, Builders<NotificacionWhatsApp>.Filter.Where(filter));
}
