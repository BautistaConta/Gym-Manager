using GymApi.Models.Roles;
using GymManager.API.Models;
using GymManager.API.Options;
using GymManager.API.Repositories;
using GymManager.API.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GyMApi.Tests.Authentication;

public class AdminBootstrapperTests
{
    [Fact]
    public async Task Existing_admin_is_never_replaced()
    {
        var repository = new BootstrapUserRepository { HasAdmin = true };
        var bootstrapper = CreateBootstrapper(repository);

        await bootstrapper.RunAsync();

        Assert.Empty(repository.CreatedUsers);
        Assert.Empty(repository.UpdatedUsers);
    }

    [Fact]
    public async Task Enabled_bootstrap_creates_only_the_first_admin()
    {
        var repository = new BootstrapUserRepository();
        var bootstrapper = CreateBootstrapper(repository);

        await bootstrapper.RunAsync();

        var created = Assert.Single(repository.CreatedUsers);
        Assert.Equal(RolUsuario.Admin, created.Rol);
        Assert.True(BCrypt.Net.BCrypt.Verify("a-secure-password", created.PasswordHash));
    }

    [Fact]
    public async Task Existing_email_is_never_promoted_or_given_a_new_password()
    {
        var existing = new Usuario
        {
            GymId = "gym-a",
            Email = "admin@example.com",
            EmailNormalizado = "admin@example.com",
            PasswordHash = "original-hash",
            Rol = RolUsuario.Gestor
        };
        var repository = new BootstrapUserRepository { ExistingByEmail = existing };

        await CreateBootstrapper(repository).RunAsync();

        Assert.Empty(repository.CreatedUsers);
        Assert.Empty(repository.UpdatedUsers);
        Assert.Equal("original-hash", existing.PasswordHash);
        Assert.Equal(RolUsuario.Gestor, existing.Rol);
    }

    private static AdminBootstrapper CreateBootstrapper(BootstrapUserRepository repository)
    {
        var options = Options.Create(new BootstrapAdminOptions
        {
            Enabled = true,
            Nombre = "Primer Admin",
            Email = "admin@example.com",
            Password = "a-secure-password"
        });
        return new AdminBootstrapper(repository, new UserService(repository), options, NullLogger<AdminBootstrapper>.Instance);
    }
}

internal sealed class BootstrapUserRepository : IUserRepository
{
    public bool HasAdmin { get; set; }
    public Usuario? ExistingByEmail { get; set; }
    public List<Usuario> CreatedUsers { get; } = [];
    public List<Usuario> UpdatedUsers { get; } = [];

    public Task<Usuario?> GetByEmailAsync(string email) => Task.FromResult(ExistingByEmail);
    public Task<Usuario?> GetByIdAsync(string id) => Task.FromResult<Usuario?>(null);
    public Task<List<Usuario>> GetAllAsync() => Task.FromResult(new List<Usuario>());
    public Task<bool> AnyAdminAsync() => Task.FromResult(HasAdmin);
    public Task CreateAsync(Usuario user) { CreatedUsers.Add(user); HasAdmin = user.Rol == RolUsuario.Admin; return Task.CompletedTask; }
    public Task UpdateAsync(Usuario user) { UpdatedUsers.Add(user); return Task.CompletedTask; }
}
