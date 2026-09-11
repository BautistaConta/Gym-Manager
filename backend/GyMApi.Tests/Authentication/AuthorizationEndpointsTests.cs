using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GymApi.Models.Roles;
using GymManager.API.Models;
using GymManager.API.Repositories;
using GymManager.API.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GyMApi.Tests.Authentication;

public class AuthorizationEndpointsTests : IClassFixture<SecureApiFactory>
{
    private readonly SecureApiFactory _factory;

    public AuthorizationEndpointsTests(SecureApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Public_register_is_not_available_outside_development()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            nombre = "Usuario",
            email = "usuario@example.com",
            password = "password-seguro"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Legacy_seed_admin_endpoint_does_not_exist()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/users/seed-admin", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_user_cannot_list_users()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Gestor_cannot_administer_users()
    {
        using var client = CreateAuthenticatedClient("Gestor");

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Gestor_cannot_create_administrative_users()
    {
        using var client = CreateAuthenticatedClient("Gestor");

        var response = await client.PostAsJsonAsync("/api/users/crear-empleado", new
        {
            nombre = "Otro gestor",
            email = "gestor@example.com",
            password = "password-seguro",
            rol = "Gestor"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Gestor_cannot_change_administrative_roles()
    {
        using var client = CreateAuthenticatedClient("Gestor");

        var response = await client.PutAsJsonAsync(
            "/api/users/507f1f77bcf86cd799439011/rol",
            new { nuevoRol = "Gestor" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_list_users_from_its_gym()
    {
        using var client = CreateAuthenticatedClient("Admin");

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("admin@example.com", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_can_create_administrative_users()
    {
        using var client = CreateAuthenticatedClient("Admin");

        var response = await client.PostAsJsonAsync("/api/users/crear-empleado", new
        {
            nombre = "Otro gestor",
            email = "gestor@example.com",
            password = "password-seguro",
            rol = "Gestor"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_change_administrative_roles()
    {
        using var client = CreateAuthenticatedClient("Admin");

        var response = await client.PutAsJsonAsync(
            "/api/users/507f1f77bcf86cd799439011/rol",
            new { nuevoRol = "Gestor" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }
}

public sealed class SecureApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDB:DatabaseName"] = "tests",
            ["MongoDB:UsersCollectionName"] = "Usuarios",
            ["Jwt:Key"] = "testing-key-with-at-least-32-characters-123",
            ["Jwt:Issuer"] = "tests",
            ["Jwt:Audience"] = "tests",
            ["MultiTenancy:PilotGymId"] = "gym-a",
            ["Cors:AllowedOrigins:0"] = "https://frontend.example.test",
            ["Twilio:Enabled"] = "false",
            ["BootstrapAdmin:Enabled"] = "false"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IUserRepository, FakeUserRepository>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "507f1f77bcf86cd799439011"),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(GymClaims.GymId, "gym-a")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly Usuario _admin = new()
    {
        Id = "507f1f77bcf86cd799439011",
        GymId = "gym-a",
        Nombre = "Admin",
        Email = "admin@example.com",
        EmailNormalizado = "admin@example.com",
        PasswordHash = "not-used",
        Rol = RolUsuario.Admin
    };

    public Task<Usuario?> GetByEmailAsync(string email) => Task.FromResult<Usuario?>(_admin.Email == email ? _admin : null);
    public Task<Usuario?> GetByIdAsync(string id) => Task.FromResult<Usuario?>(_admin.Id == id ? _admin : null);
    public Task<List<Usuario>> GetAllAsync() => Task.FromResult(new List<Usuario> { _admin });
    public Task<bool> AnyAdminAsync() => Task.FromResult(true);
    public Task CreateAsync(Usuario user) => Task.CompletedTask;
    public Task UpdateAsync(Usuario user) => Task.CompletedTask;
}
