using GymManager.API.Data;
using GymManager.API.Models;
using MongoDB.Driver;
using MongoDB.Bson;

namespace GymManager.API.Repositories
{
    public class CategoriaPagoRepository
    {
        private readonly IMongoCollection<CategoriaPago> _collection;

        public CategoriaPagoRepository(MongoDbContext context)
        {
            _collection = context.CategoriasPago;
        }

        public async Task CreateAsync(CategoriaPago categoria)
        {
            await _collection.InsertOneAsync(categoria);
        }

        public async Task<List<CategoriaPago>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<CategoriaPago?> GetByIdAsync(string id)
        {
            return await _collection.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(CategoriaPago categoria)
        {
            await _collection.ReplaceOneAsync(c => c.Id == categoria.Id, categoria);
        }

        public async Task DeleteLegacyWithoutIdAsync() =>
            await _collection.DeleteOneAsync(Builders<CategoriaPago>.Filter.Eq("_id", BsonNull.Value));
    }
}
