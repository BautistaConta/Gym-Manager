using GyMApi.Models;
using MongoDB.Driver;

namespace GymManager.API.Repositories
{
    public class SucursalRepository
    {
        private readonly IMongoCollection<Sucursal> _collection;

        public SucursalRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<Sucursal>("Sucursales");
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
    }
}
