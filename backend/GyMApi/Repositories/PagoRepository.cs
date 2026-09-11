using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public class PagoRepository
{
    private readonly IMongoCollection<Pago> _collection;
    private readonly IGymContext _gymContext;

    public PagoRepository(MongoDbContext context, IGymContext gymContext)
    {
        _collection = context.Pagos;
        _gymContext = gymContext;
    }

    public Task CreateAsync(Pago pago)
    {
        TenantFilters.Stamp(pago, _gymContext.GymId);
        return _collection.InsertOneAsync(pago);
    }

    public Task<List<Pago>> GetAllAsync() => _collection
        .Find(TenantFilters.ForGym<Pago>(_gymContext.GymId)).SortByDescending(p => p.FechaPago).ToListAsync();

    public async Task<Pago?> GetByIdAsync(string id) => await _collection.Find(ById(id)).FirstOrDefaultAsync();

    public Task<List<Pago>> GetByAlumnoIdAsync(string alumnoId) => _collection
        .Find(With(p => p.AlumnoId == alumnoId)).SortByDescending(p => p.FechaPago).ToListAsync();

    public async Task<Pago?> GetUltimoPagoAsync(string alumnoId) => await _collection
        .Find(With(p => p.AlumnoId == alumnoId)).SortByDescending(p => p.PeriodoHasta).FirstOrDefaultAsync();

    public Task<List<Pago>> GetBySucursalIdAsync(string sucursalId) =>
        _collection.Find(With(p => p.SucursalId == sucursalId)).ToListAsync();

    public Task<long> CountBySucursalIdAsync(string sucursalId) =>
        _collection.CountDocumentsAsync(With(p => p.SucursalId == sucursalId));

    public Task<List<Pago>> GetByRangoFechasAsync(DateTime desde, DateTime hasta) =>
        _collection.Find(With(p => p.FechaPago >= desde && p.FechaPago <= hasta)).ToListAsync();

    private FilterDefinition<Pago> ById(string id) => With(p => p.Id == id);
    private FilterDefinition<Pago> With(System.Linq.Expressions.Expression<Func<Pago, bool>> filter) =>
        TenantFilters.And<Pago>(_gymContext.GymId, Builders<Pago>.Filter.Where(filter));
}
