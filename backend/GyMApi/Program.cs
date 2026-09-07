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

// Registraciones
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<SucursalRepository>();
builder.Services.AddSingleton<CategoriaPagoRepository>();
builder.Services.AddSingleton<AlumnoRepository>();
builder.Services.AddSingleton<PagoRepository>();
builder.Services.AddSingleton<INotificacionRepository, NotificacionRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PagoService>();
builder.Services.AddScoped<AlumnoService>();
builder.Services.AddScoped<CategoriaPagoService>();
builder.Services.AddScoped<SucursalService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddSingleton<JwtService>();
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
var key = builder.Configuration["Jwt:Key"];
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
    });

builder.Services.AddAuthorization();

var app = builder.Build();

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
