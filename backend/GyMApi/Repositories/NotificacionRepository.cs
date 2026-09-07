using GymManager.API.Data;
using GymManager.API.Models;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public class NotificacionRepository : INotificacionRepository
{
    private readonly IMongoCollection<NotificacionWhatsApp> _collection;
    public NotificacionRepository(MongoDbContext context) => _collection = context.NotificacionesWhatsApp;
    public Task CreateAsync(NotificacionWhatsApp notificacion) => _collection.InsertOneAsync(notificacion);
    public async Task<List<NotificacionWhatsApp>> GetAllAsync(EstadoNotificacionWhatsApp? estado, string? alumnoId)
    {
        var filter = Builders<NotificacionWhatsApp>.Filter.Empty;
        if (estado.HasValue) filter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.Estado, estado.Value);
        if (!string.IsNullOrWhiteSpace(alumnoId)) filter &= Builders<NotificacionWhatsApp>.Filter.Eq(n => n.AlumnoId, alumnoId);
        return await _collection.Find(filter).SortByDescending(n => n.FechaCreacion).ToListAsync();
    }
    public async Task<NotificacionWhatsApp?> GetByIdAsync(string id) => await _collection.Find(n => n.Id == id).FirstOrDefaultAsync();
    public Task<bool> ExistsSinceAsync(string alumnoId, TipoNotificacionWhatsApp tipo, DateTime desde) =>
        _collection.Find(n => n.AlumnoId == alumnoId && n.Tipo == tipo && n.FechaCreacion >= desde).AnyAsync();
    public Task<bool> ExistsEnviadaDesdeAsync(string alumnoId, TipoNotificacionWhatsApp tipo, DateTime desde) =>
        _collection.Find(n => n.AlumnoId == alumnoId && n.Tipo == tipo && n.Estado == EstadoNotificacionWhatsApp.Enviado && n.FechaCreacion >= desde).AnyAsync();
    public Task UpdateAsync(NotificacionWhatsApp notificacion) => _collection.ReplaceOneAsync(n => n.Id == notificacion.Id, notificacion);
}
