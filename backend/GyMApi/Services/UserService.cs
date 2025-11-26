using GymApi.Models.Roles;
using GymManager.API.Models;
using GymManager.API.Repositories;

namespace GymManager.API.Services
{
    public class UserService
    {
        private readonly UserRepository _repo;

        public UserService(UserRepository repo)
        {
            _repo = repo;
        }

        public async Task<Usuario> RegisterAsync(RegisterRequest request,RolUsuario rol)
        {
            var existing = await _repo.GetByEmailAsync(request.Email);
            if (existing != null)
                throw new Exception("El email ya está registrado.");

            var user = new Usuario
            {
                Nombre = request.Nombre,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Rol = rol
            };

            await _repo.CreateAsync(user);
            return user;
        }

        public async Task<Usuario> LoginAsync(LoginRequest request)
        {
            var user = await _repo.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Credenciales inválidas.");

            return user;
        }
    }
}
