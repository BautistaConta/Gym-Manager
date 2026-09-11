using GymApi.Models.Roles;
using GymManager.API.Models;
using GymManager.API.Repositories;
using GymManager.API.DTOs;

namespace GymManager.API.Services
{
    public class UserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<Usuario> LoginAsync(LoginRequest request)
        {
            var user = await _repo.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new DomainException("Credenciales inválidas.");

            return user;
        }
        public async Task<Usuario> CrearUsuarioAsync(CrearUsuarioRequest request)
{
    ValidateAdministrativeRole(request.Rol, out var rol);
    ValidateAdministrativeUser(request.Nombre, request.Email, request.Password);

    var existing = await _repo.GetByEmailAsync(request.Email);
    if (existing != null)
        throw new DomainException("El email ya está registrado.");

    var usuario = new Usuario
    {
        Nombre = request.Nombre,
        Email = request.Email.Trim(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Rol = rol,
        FechaAlta = DateTime.UtcNow
    };

    await _repo.CreateAsync(usuario);
    return usuario;
}

        public async Task<Usuario> CambiarRolAsync(string id, string nuevoRol)
        {
            ValidateAdministrativeRole(nuevoRol, out var rol);
            var usuario = await _repo.GetByIdAsync(id) ?? throw new DomainException("Usuario no encontrado.");
            usuario.Rol = rol;
            await _repo.UpdateAsync(usuario);
            return usuario;
        }

        private static void ValidateAdministrativeRole(string value, out RolUsuario rol)
        {
            if (!Enum.TryParse(value, true, out rol) || rol is not (RolUsuario.Admin or RolUsuario.Gestor))
                throw new DomainException("El rol debe ser Admin o Gestor.");
        }

        private static void ValidateAdministrativeUser(string nombre, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email))
                throw new DomainException("Nombre y email son obligatorios.");
            if (password.Length < 10)
                throw new DomainException("La contraseña debe tener al menos 10 caracteres.");
        }

    }
}
