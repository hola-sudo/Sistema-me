using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaClinico.Core.Interfaces;
using SistemaClinico.Infrastructure.Data;
using SistemaClinico.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Validar connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured. Please set it in appsettings.json or environment variables.");

// Configurar DbContext
// Agrega el DbContext con SQL Server
/*builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));*/


// Agrega el DbContext con SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 1. Configura la política CORS
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // En desarrollo, permite cualquier origen
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    }
    else
    {
        // En producción, restringe a orígenes específicos
        var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',') 
            ?? new[] { "http://localhost:4200" };
        
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    }
});
// Configurar servicios de inyección de dependencias
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IHistoriaClinicaService, HistoriaClinicaService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IDotoresService, DotoresService>();
builder.Services.AddScoped<IRolService, RolService>();

// Configurar autenticación JWT
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT Key is not configured. Please set 'Jwt:Key' in appsettings.json or environment variables.");

if (string.IsNullOrEmpty(jwtIssuer))
    throw new InvalidOperationException("JWT Issuer is not configured. Please set 'Jwt:Issuer' in appsettings.json or environment variables.");

if (string.IsNullOrEmpty(jwtAudience))
    throw new InvalidOperationException("JWT Audience is not configured. Please set 'Jwt:Audience' in appsettings.json or environment variables.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };
    });

builder.Services.AddAuthorization();

// Cargar controladores
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema Clínico API",
        Version = "v1",
        Description = "API REST para la gestión del sistema clínico"
    });

    // Si usarás JWT más adelante, puedes preparar esto:
    // options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
});


var app = builder.Build();

// Inicializar base de datos si no existe
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Asegurar que la base de datos se crea si no existe
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// 2. Usa CORS en la app
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Clínico API v1");
        options.RoutePrefix = string.Empty; // Swagger en la raíz (/)
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
