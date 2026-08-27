using GymManager.API.Data;
using GymManager.API.Models;
using MongoDB.Driver;
using MongoDB.Bson;

namespace GymManager.API.Repositories
{
    public class SucursalRepository
    {
        private readonly IMongoCollection<Sucursal> _collection;

        public SucursalRepository(MongoDbContext context)
        {
            _collection = context.Sucursales;
        }

        public async Task CreateAsync(Sucursal sucursal)
        {
            await _collection.InsertOneAsync(sucursal);
        }

        public async Task<List<Sucursal>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<Sucursal?> GetByIdAsync(string id)
        {
            return await _collection.Find(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Sucursal sucursal) =>
            await _collection.ReplaceOneAsync(s => s.Id == sucursal.Id, sucursal);

        public async Task DeleteAsync(string id) =>
            await _collection.DeleteOneAsync(s => s.Id == id);

        public async Task DeleteLegacyWithoutIdAsync() =>
            await _collection.DeleteOneAsync(Builders<Sucursal>.Filter.Eq("_id", BsonNull.Value));
    }
}
