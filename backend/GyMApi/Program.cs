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
builder.Services.AddOptions<MultiTenancyOptions>()
    .Bind(builder.Configuration.GetSection(MultiTenancyOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.PilotGymId), "MultiTenancy:PilotGymId es obligatorio.")
    .ValidateOnStart();
builder.Services.AddScoped<IGymContext, GymContext>();

// Registraciones
builder.Services.AddScoped<UserRepository>();
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
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<MongoIndexInitializer>();
builder.Services.AddSingleton<PilotGymMigration>();
builder.Services.Configure<TwilioOptions>(builder.Configuration.GetSection(TwilioOptions.SectionName));
builder.Services.AddHttpClient<IWhatsAppSender, TwilioWhatsAppSender>(client => client.BaseAddress = new Uri("https://api.twilio.com/"));
builder.Services.AddHostedService<VencimientosNotificacionJob>();
builder.Services.AddHostedService<EnviarNotificacionesJob>();

// CORS - permitir el frontend (cambia origen si es necesario)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterApp",
        policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// JWT Authentication
var key = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no está configurado.");
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // en prod true
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            RoleClaimType="http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
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

await app.Services.GetRequiredService<MongoIndexInitializer>().EnsureCreatedAsync();

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
