using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public class SucursalRepository
{
    private readonly IMongoCollection<Sucursal> _collection;
    private readonly IGymContext _gymContext;

    public SucursalRepository(MongoDbContext context, IGymContext gymContext)
    {
        _collection = context.Sucursales;
        _gymContext = gymContext;
    }

    public Task CreateAsync(Sucursal sucursal)
    {
        TenantFilters.Stamp(sucursal, _gymContext.GymId);
        return _collection.InsertOneAsync(sucursal);
    }

    public Task<List<Sucursal>> GetAllAsync() =>
        _collection.Find(TenantFilters.ForGym<Sucursal>(_gymContext.GymId)).ToListAsync();

    public async Task<Sucursal?> GetByIdAsync(string id) => await _collection.Find(ById(id)).FirstOrDefaultAsync();

    public Task UpdateAsync(Sucursal sucursal)
    {
        TenantFilters.EnsureOwned(sucursal, _gymContext.GymId);
        return _collection.ReplaceOneAsync(ById(sucursal.Id), sucursal);
    }

    public Task DeleteAsync(string id) => _collection.DeleteOneAsync(ById(id));

    public Task DeleteLegacyWithoutIdAsync() => _collection.DeleteOneAsync(
        TenantFilters.And<Sucursal>(_gymContext.GymId, Builders<Sucursal>.Filter.Eq("_id", BsonNull.Value)));

    private FilterDefinition<Sucursal> ById(string id) =>
        TenantFilters.And<Sucursal>(_gymContext.GymId, Builders<Sucursal>.Filter.Eq(s => s.Id, id));
}
