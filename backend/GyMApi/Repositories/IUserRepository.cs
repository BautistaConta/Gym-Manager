using GymManager.API.Models;

namespace GymManager.API.Repositories;

public interface IUserRepository
{
    Task<Usuario?> GetByEmailAsync(string email);
    Task<Usuario?> GetByIdAsync(string id);
    Task<List<Usuario>> GetAllAsync();
    Task<bool> AnyAdminAsync();
    Task CreateAsync(Usuario user);
    Task UpdateAsync(Usuario user);
}
