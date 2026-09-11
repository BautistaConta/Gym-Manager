using GymManager.API.Data;
using GymManager.API.Repositories;
using GymManager.API.Services;
using GymManager.API.Jobs;
using GymManager.API.Options;
using GymManager.API.Senders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using GymManager.API.Tenancy;
using GymManager.API.Migrations;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Gym API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegá únicamente el token JWT obtenido en /api/Auth/login."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOptions<MongoDbOptions>()
    .Bind(builder.Configuration.GetSection(MongoDbOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "MongoDB:ConnectionString es obligatorio.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName), "MongoDB:DatabaseName es obligatorio.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.UsersCollectionName), "MongoDB:UsersCollectionName es obligatorio.")
    .ValidateOnStart();
builder.Services.AddOptions<MultiTenancyOptions>()
    .Bind(builder.Configuration.GetSection(MultiTenancyOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.PilotGymId), "MultiTenancy:PilotGymId es obligatorio.")
    .ValidateOnStart();
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Key) && options.Key.Length >= 32, "Jwt:Key debe tener al menos 32 caracteres.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer es obligatorio.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience es obligatorio.")
    .ValidateOnStart();
builder.Services.AddOptions<BootstrapAdminOptions>()
    .Bind(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName))
    .Validate(options => !options.Enabled || (!string.IsNullOrWhiteSpace(options.Nombre) && !string.IsNullOrWhiteSpace(options.Email) && options.Password.Length >= 10),
        "Si BootstrapAdmin está habilitado, Nombre, Email y Password de al menos 10 caracteres son obligatorios.")
    .ValidateOnStart();
builder.Services.AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .Validate(options => builder.Environment.IsDevelopment() ||
        (options.AllowedOrigins.Length > 0 && options.AllowedOrigins.All(origin =>
            !string.IsNullOrWhiteSpace(origin) && origin != "*" &&
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)),
        "Cors:AllowedOrigins debe contener orígenes HTTPS concretos en entornos no Development.")
    .ValidateOnStart();
builder.Services.AddScoped<IGymContext, GymContext>();

// Registraciones
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IUserRepository>(provider => provider.GetRequiredService<UserRepository>());
builder.Services.AddScoped<SucursalRepository>();
builder.Services.AddScoped<CategoriaPagoRepository>();
builder.Services.AddScoped<AlumnoRepository>();
builder.Services.AddScoped<PagoRepository>();
builder.Services.AddScoped<INotificacionRepository, NotificacionRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PagoService>();
builder.Services.AddScoped<AlumnoService>();
builder.Services.AddScoped<CategoriaPagoService>();
builder.Services.AddScoped<SucursalService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddScoped<AdminBootstrapper>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<MongoIndexInitializer>();
builder.Services.AddSingleton<PilotGymMigration>();
builder.Services.AddOptions<TwilioOptions>()
    .Bind(builder.Configuration.GetSection(TwilioOptions.SectionName))
    .Validate(options => !options.Enabled ||
        (!string.IsNullOrWhiteSpace(options.AccountSid) &&
         !string.IsNullOrWhiteSpace(options.AuthToken) &&
         !string.IsNullOrWhiteSpace(options.WhatsAppFromNumber) &&
         !string.IsNullOrWhiteSpace(options.ContentSid)),
        "La configuración de Twilio está incompleta mientras Twilio:Enabled=true.")
    .ValidateOnStart();
builder.Services.AddHttpClient<IWhatsAppSender, TwilioWhatsAppSender>(client => client.BaseAddress = new Uri("https://api.twilio.com/"));
builder.Services.AddHostedService<VencimientosNotificacionJob>();
builder.Services.AddHostedService<EnviarNotificacionesJob>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterApp", policy =>
    {
        if (builder.Environment.IsDevelopment()) policy.AllowAnyOrigin();
        else policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []);
        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("La configuración JWT está incompleta.");
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            RoleClaimType="http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Principal?.FindFirst(GymClaims.GymId)?.Value))
                    context.Fail("El token no contiene gym_id.");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (args.Contains("--migrate-pilot-gym", StringComparer.OrdinalIgnoreCase))
{
    await app.Services.GetRequiredService<PilotGymMigration>().RunAsync();
    await app.Services.GetRequiredService<MongoIndexInitializer>().EnsureCreatedAsync();
    return;
}

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.GetRequiredService<MongoIndexInitializer>().EnsureCreatedAsync();
    using var bootstrapScope = app.Services.CreateScope();
    await bootstrapScope.ServiceProvider.GetRequiredService<AdminBootstrapper>().RunAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFlutterApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
