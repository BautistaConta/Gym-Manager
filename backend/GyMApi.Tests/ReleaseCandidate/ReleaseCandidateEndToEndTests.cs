using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using GymManager.API.Data;
using GymManager.API.Migrations;
using GymManager.API.Models;
using GymManager.API.Services;
using GymManager.API.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace GyMApi.Tests.ReleaseCandidate;

public sealed class ReleaseCandidateEndToEndTests
{
    [Fact]
    public async Task Pilot_http_flow_works_against_an_isolated_temporary_database()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_RC_E2E"), "true", StringComparison.OrdinalIgnoreCase))
            return;
        var connection = Environment.GetEnvironmentVariable("RC_MONGO_CONNECTION_STRING");
        Assert.False(string.IsNullOrWhiteSpace(connection));
        var databaseName = $"GymRC_{Guid.NewGuid():N}"[..26];
        using var factory = new RcFactory(connection!, databaseName);
        try
        {
            using var client = factory.CreateClient();
            await factory.Services.GetRequiredService<MongoIndexInitializer>().EnsureCreatedAsync();
            using (var bootstrapScope = factory.Services.CreateScope())
                await bootstrapScope.ServiceProvider.GetRequiredService<AdminBootstrapper>().RunAsync();

            var login = await client.PostAsJsonAsync("/api/auth/login", new
                { email = "rc-admin@example.test", password = "RcAdmin12345!" });
            login.EnsureSuccessStatusCode();
            var loginJson = await login.Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", loginJson.GetProperty("token").GetString());

            var gestor = await client.PostAsJsonAsync("/api/users/crear-empleado", new
            {
                nombre = "Gestor RC", email = "rc-gestor@example.test",
                password = "RcGestor12345!", rol = "Gestor"
            });
            gestor.EnsureSuccessStatusCode();

            var branches = new List<string>();
            for (var i = 1; i <= 4; i++)
            {
                var response = await client.PostAsJsonAsync("/api/sucursales", new
                    { nombre = $"Sucursal RC {i}", direccion = $"Dirección {i}" });
                response.EnsureSuccessStatusCode();
                branches.Add((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!);
            }
            var branchList = await client.GetFromJsonAsync<JsonElement>("/api/sucursales");
            Assert.Equal(4, branchList.GetArrayLength());

            var categoryResponse = await client.PostAsJsonAsync("/api/categorias-pago", new
                { nombre = "Mensual RC", precio = 15000, mesesDuracion = 1, tipoAbono = 0 });
            categoryResponse.EnsureSuccessStatusCode();
            var categoryId = (await categoryResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

            var students = new List<string>();
            for (var i = 1; i <= 5; i++)
            {
                var consent = i != 4;
                var response = await client.PostAsJsonAsync("/api/alumnos", new
                {
                    nombre = $"Alumno RC {i}", dni = $"9900000{i}", telefono = $"+54911000000{i:00}",
                    sucursalPrincipalId = branches[(i - 1) % branches.Count],
                    notificacionesHabilitadas = consent, consentimientoConfirmado = consent,
                    medioConsentimiento = consent ? "prueba RC" : null
                });
                response.EnsureSuccessStatusCode();
                students.Add((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!);
            }

            var edit = await client.PutAsJsonAsync($"/api/alumnos/{students[0]}", new
                { nombre = "Alumno RC Editado", telefono = "+5491100000099", activo = true, sucursalPrincipalId = branches[1] });
            edit.EnsureSuccessStatusCode();
            var search = await client.GetFromJsonAsync<JsonElement>("/api/alumnos/search?nombre=Editado");
            Assert.Single(search.EnumerateArray());

            var endInWindow = DateTime.UtcNow.Date.AddDays(5);
            var paymentIds = new List<string>();
            for (var method = 0; method < 3; method++)
            {
                var response = await client.PostAsJsonAsync("/api/pagos", new
                {
                    alumnoId = students[method], sucursalId = branches[method], categoriaPagoId = categoryId,
                    metodoPago = method, descuentoPorcentaje = 0,
                    periodoHastaManual = method == 0 ? endInWindow : (DateTime?)null
                });
                response.EnsureSuccessStatusCode();
                paymentIds.Add((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!);
            }

            var lateStudentResponse = await client.PostAsJsonAsync("/api/alumnos", new
            {
                nombre = "Alumno Renovación Atrasada", dni = "99000009", telefono = "+5491100000098",
                sucursalPrincipalId = branches[0], notificacionesHabilitadas = false,
                consentimientoConfirmado = false, medioConsentimiento = (string?)null
            });
            lateStudentResponse.EnsureSuccessStatusCode();
            var lateStudentId = (await lateStudentResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
            DateTime businessToday;
            using (var dataScope = factory.Services.CreateScope())
            {
                var db = dataScope.ServiceProvider.GetRequiredService<MongoDbContext>();
                businessToday = dataScope.ServiceProvider.GetRequiredService<CuotaCalculator>()
                    .Hoy.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                await db.Pagos.InsertOneAsync(new Pago
                {
                    GymId = "pilot-rc", AlumnoId = lateStudentId, SucursalId = branches[0],
                    CategoriaPagoId = categoryId, PrecioCategoria = 15000, MontoFinal = 15000,
                    MetodoPago = MetodoPago.Efectivo, FechaPago = businessToday.AddDays(-40),
                    PeriodoDesde = businessToday.AddDays(-40), PeriodoHasta = businessToday.AddDays(-10)
                });
            }
            var lateRenewal = await client.PostAsJsonAsync("/api/pagos", new
            {
                alumnoId = lateStudentId, sucursalId = branches[0], categoriaPagoId = categoryId,
                metodoPago = 0, descuentoPorcentaje = 0, periodoHastaManual = (DateTime?)null
            });
            lateRenewal.EnsureSuccessStatusCode();
            Assert.Equal(businessToday,
                (await lateRenewal.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("periodoDesde").GetDateTime());

            var inactivePayment = await client.PostAsJsonAsync("/api/pagos", new
            {
                alumnoId = students[4], sucursalId = branches[0], categoriaPagoId = categoryId,
                metodoPago = 0, descuentoPorcentaje = 0, periodoHastaManual = endInWindow
            });
            inactivePayment.EnsureSuccessStatusCode();
            var inactivePaymentId = (await inactivePayment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/alumnos/{students[4]}")).StatusCode);

            var reminder = await client.PostAsync($"/api/pagos/{paymentIds[0]}/recordatorio", null);
            reminder.EnsureSuccessStatusCode();
            (await client.PostAsync($"/api/pagos/{paymentIds[0]}/recordatorio", null)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsync($"/api/pagos/{inactivePaymentId}/recordatorio", null)).StatusCode);

            var noConsentPayment = await client.PostAsJsonAsync("/api/pagos", new
            {
                alumnoId = students[3], sucursalId = branches[3], categoriaPagoId = categoryId,
                metodoPago = 1, descuentoPorcentaje = 0, periodoHastaManual = endInWindow
            });
            noConsentPayment.EnsureSuccessStatusCode();
            var noConsentPaymentId = (await noConsentPayment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsync($"/api/pagos/{noConsentPaymentId}/recordatorio", null)).StatusCode);

            using (var processingScope = factory.Services.CreateScope())
            {
                var accessor = processingScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                accessor.HttpContext = AuthenticatedContext("pilot-rc");
                await processingScope.ServiceProvider.GetRequiredService<NotificacionService>()
                    .ProcesarPendientesAsync(10);
            }

            var history = await client.GetFromJsonAsync<JsonElement>("/api/notificaciones?tipo=0&pagina=1&tamanoPagina=20");
            Assert.Equal(1, history.GetProperty("total").GetInt64());
            Assert.Equal(6, history.GetProperty("items")[0].GetProperty("estado").GetInt32()); // Simulado

            using (var dataScope = factory.Services.CreateScope())
            {
                var db = dataScope.ServiceProvider.GetRequiredService<MongoDbContext>();
                var foreign = new Alumno
                {
                    GymId = "otro-gym", Nombre = "Alumno Ajeno", DNI = "11111111",
                    Telefono = "+5491199999999", Activo = true, FechaAlta = DateTime.UtcNow
                };
                await db.Alumnos.InsertOneAsync(foreign);
                Assert.Equal(0, await db.NotificacionesWhatsApp.CountDocumentsAsync(n =>
                    n.GymId == "pilot-rc" && n.Tipo == TipoNotificacionWhatsApp.Vencido));
                Assert.Equal(0, await db.NotificacionesWhatsApp.CountDocumentsAsync(n =>
                    n.GymId == "pilot-rc" && n.Tipo == TipoNotificacionWhatsApp.PagoConfirmado));
            }
            var visibleStudents = await client.GetFromJsonAsync<JsonElement>("/api/alumnos");
            Assert.DoesNotContain(visibleStudents.EnumerateArray(), item =>
                item.GetProperty("nombre").GetString() == "Alumno Ajeno");
        }
        finally
        {
            Assert.StartsWith("GymRC_", databaseName, StringComparison.Ordinal);
            await new MongoClient(connection).DropDatabaseAsync(databaseName);
        }
    }

    private static DefaultHttpContext AuthenticatedContext(string gymId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "rc-admin"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(GymClaims.GymId, gymId)
        ], "RC"));
        return context;
    }

    private sealed class RcFactory(string connection, string databaseName) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MongoDB:ConnectionString"] = connection,
                    ["MongoDB:DatabaseName"] = databaseName,
                    ["MongoDB:UsersCollectionName"] = "Usuarios",
                    ["Jwt:Key"] = "rc-only-key-with-at-least-32-characters-12345",
                    ["Jwt:Issuer"] = "gymmanager.rc",
                    ["Jwt:Audience"] = "gymmanager.rc",
                    ["MultiTenancy:PilotGymId"] = "pilot-rc",
                    ["BootstrapAdmin:Enabled"] = "true",
                    ["BootstrapAdmin:Nombre"] = "Admin RC",
                    ["BootstrapAdmin:Email"] = "rc-admin@example.test",
                    ["BootstrapAdmin:Password"] = "RcAdmin12345!",
                    ["Cors:AllowedOrigins:0"] = "https://rc.example.test",
                    ["Twilio:Enabled"] = "false"
                }));
        }
    }
}
