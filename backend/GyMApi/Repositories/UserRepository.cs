using GymManager.API.Data;
using GymManager.API.Models;
using GymManager.API.Tenancy;
using MongoDB.Driver;

namespace GymManager.API.Repositories;

public class UserRepository
{
    private readonly IMongoCollection<Usuario> _usuarios;
    private readonly IGymContext _gymContext;

    public UserRepository(MongoDbContext context, IGymContext gymContext)
    {
        _usuarios = context.Usuarios;
        _gymContext = gymContext;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<Usuario?> GetByEmailAsync(string email) => await _usuarios.Find(
        TenantFilters.And<Usuario>(_gymContext.GymId,
            Builders<Usuario>.Filter.Eq(u => u.EmailNormalizado, NormalizeEmail(email)))).FirstOrDefaultAsync();

    public async Task<Usuario?> GetByIdAsync(string id) => await _usuarios.Find(ById(id)).FirstOrDefaultAsync();

    public Task CreateAsync(Usuario user)
    {
        TenantFilters.Stamp(user, _gymContext.GymId);
        user.Email = user.Email.Trim();
        user.EmailNormalizado = NormalizeEmail(user.Email);
        return _usuarios.InsertOneAsync(user);
    }

    public Task<List<Usuario>> GetAllAsync() =>
        _usuarios.Find(TenantFilters.ForGym<Usuario>(_gymContext.GymId)).ToListAsync();

    public Task UpdateAsync(Usuario user)
    {
        TenantFilters.EnsureOwned(user, _gymContext.GymId);
        user.EmailNormalizado = NormalizeEmail(user.Email);
        return _usuarios.ReplaceOneAsync(ById(user.Id), user);
    }

    private FilterDefinition<Usuario> ById(string id) =>
        TenantFilters.And<Usuario>(_gymContext.GymId, Builders<Usuario>.Filter.Eq(u => u.Id, id));
}
