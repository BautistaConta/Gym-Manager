using GymApi.Models.Roles;
using GymManager.API.DTOs;
using GymManager.API.Options;
using GymManager.API.Repositories;
using Microsoft.Extensions.Options;

namespace GymManager.API.Services;

public sealed class AdminBootstrapper
{
    private readonly IUserRepository _users;
    private readonly UserService _userService;
    private readonly BootstrapAdminOptions _options;
    private readonly ILogger<AdminBootstrapper> _logger;

    public AdminBootstrapper(
        IUserRepository users,
        UserService userService,
        IOptions<BootstrapAdminOptions> options,
        ILogger<AdminBootstrapper> logger)
    {
        _users = users;
        _userService = userService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        if (!_options.Enabled) return;

        if (await _users.AnyAdminAsync())
        {
            _logger.LogInformation("Bootstrap de administrador omitido: ya existe un administrador.");
            return;
        }

        if (await _users.GetByEmailAsync(_options.Email) is not null)
        {
            _logger.LogWarning("Bootstrap de administrador omitido: el email configurado ya pertenece a un usuario y no será modificado.");
            return;
        }

        await _userService.CrearUsuarioAsync(new CrearUsuarioRequest
        {
            Nombre = _options.Nombre,
            Email = _options.Email,
            Password = _options.Password,
            Rol = RolUsuario.Admin.ToString()
        });

        _logger.LogInformation("Primer administrador creado mediante bootstrap seguro.");
    }
}
