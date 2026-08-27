using GymManager.API.Data;
using GymManager.API.Models;
using MongoDB.Driver;

namespace GymManager.API.Repositories
{
    public class UserRepository
    {
        private readonly IMongoCollection<Usuario> _usuarios;

        public UserRepository(MongoDbContext context)
        {
            _usuarios = context.Usuarios;
        }

        public async Task<Usuario> GetByEmailAsync(string email) =>
            await _usuarios.Find(u => u.Email == email).FirstOrDefaultAsync();

        public async Task<Usuario> GetByIdAsync(string id) =>
            await _usuarios.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Usuario user) =>
            await _usuarios.InsertOneAsync(user);

        public async Task<List<Usuario>> GetAllAsync() =>
            await _usuarios.Find(_ => true).ToListAsync();
        public async Task UpdateAsync(Usuario user)
        {
            var filter = Builders<Usuario>.Filter.Eq(u => u.Id, user.Id);
            await _usuarios.ReplaceOneAsync(filter, user);
        }
    }
}
