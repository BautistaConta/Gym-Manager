using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public class CategoriaPagoRepository
{
    private readonly IMongoCollection<CategoriaPago> _collection;
    private readonly IGymContext _gymContext;

    public CategoriaPagoRepository(MongoDbContext context, IGymContext gymContext)
    {
        _collection = context.CategoriasPago;
        _gymContext = gymContext;
    }

    public Task CreateAsync(CategoriaPago categoria)
    {
        TenantFilters.Stamp(categoria, _gymContext.GymId);
        return _collection.InsertOneAsync(categoria);
    }

    public Task<List<CategoriaPago>> GetAllAsync() =>
        _collection.Find(TenantFilters.ForGym<CategoriaPago>(_gymContext.GymId)).ToListAsync();

    public async Task<CategoriaPago?> GetByIdAsync(string id) => await _collection.Find(ById(id)).FirstOrDefaultAsync();

    public Task UpdateAsync(CategoriaPago categoria)
    {
        TenantFilters.EnsureOwned(categoria, _gymContext.GymId);
        return _collection.ReplaceOneAsync(ById(categoria.Id), categoria);
    }

    public Task DeleteLegacyWithoutIdAsync() => _collection.DeleteOneAsync(
        TenantFilters.And<CategoriaPago>(_gymContext.GymId, Builders<CategoriaPago>.Filter.Eq("_id", BsonNull.Value)));

    private FilterDefinition<CategoriaPago> ById(string id) =>
        TenantFilters.And<CategoriaPago>(_gymContext.GymId, Builders<CategoriaPago>.Filter.Eq(c => c.Id, id));
}
