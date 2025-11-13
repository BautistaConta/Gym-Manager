using GymManager.API.Models;
using MongoDB.Driver;

namespace GymManager.API.Repositories
{
    public class UserRepository
    {
        private readonly IMongoCollection<Usuario> _usuarios;

        public UserRepository(IConfiguration config)
        {
            var client = new MongoClient(config["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(config["MongoDB:DatabaseName"]);
            _usuarios = database.GetCollection<Usuario>(config["MongoDB:UsersCollectionName"]);
        }

        public async Task<Usuario> GetByEmailAsync(string email) =>
            await _usuarios.Find(u => u.Email == email).FirstOrDefaultAsync();

        public async Task<Usuario> GetByIdAsync(string id) =>
            await _usuarios.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Usuario user) =>
            await _usuarios.InsertOneAsync(user);

        public async Task<List<Usuario>> GetAllAsync() =>
            await _usuarios.Find(_ => true).ToListAsync();
    }
}
