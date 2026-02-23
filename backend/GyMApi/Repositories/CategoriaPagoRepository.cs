using GyMApi.Models;
using MongoDB.Driver;

namespace GymManager.API.Repositories
{
    public class CategoriaPagoRepository
    {
        private readonly IMongoCollection<CategoriaPago> _collection;

        public CategoriaPagoRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<CategoriaPago>("CategoriasPago");
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
    }
}