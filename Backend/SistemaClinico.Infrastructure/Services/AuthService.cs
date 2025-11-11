using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SistemaClinico.Core.DTOs.Auth;
using SistemaClinico.Core.Entities;
using SistemaClinico.Core.Interfaces;
using SistemaClinico.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SistemaClinico.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        public Task<bool> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _config["Jwt:Key"];
            
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT Key is not configured.");

            var key = Encoding.UTF8.GetBytes(jwtKey);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true, // valida expiración
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                // Si no lanza excepción, el token es válido
                return Task.FromResult(true);
            }
            catch
            {
                // Token inválido o expirado
                return Task.FromResult(false);
            }
        }

        public async Task<string> GenerateTokenByUserIdAsync(int userId)
        {
            var usuario = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Id == userId);

            if (usuario == null)
                throw new UnauthorizedAccessException("Usuario no encontrado");


            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol?.Nombre ?? string.Empty)
            };

            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT Key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> LoginAsync(LoginRequestDto dto)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Correo == dto.Correo);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Clave, usuario.ClaveHash))
                throw new UnauthorizedAccessException("Credenciales inválidas");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol?.Nombre ?? string.Empty)
            };

            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT Key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<IEnumerable<UsuarioDto>> GetUsuariosAsync()
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .Select(u => new UsuarioDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Correo = u.Correo,
                    RolNombre = u.Rol != null ? u.Rol.Nombre : string.Empty,
                    Status = u.Status,
                    FechaCreacion = u.FechaCreacion
                })
                //.Where(u => u.Status == true)
                .ToListAsync();
        }

        public async Task<bool> RegisterAsync(RegisterRequestDto dto)
        {
            var exists = await _context.Usuarios.AnyAsync(u => u.Correo == dto.Correo);
            if (exists) 
                throw new InvalidOperationException("El correo ya está registrado.");

            // Buscar el rol por ID
            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == dto.RolId);
            if (rol == null)
                throw new ArgumentException($"El rol con ID {dto.RolId} no existe.");

            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Correo = dto.Correo,
                ClaveHash = BCrypt.Net.BCrypt.HashPassword(dto.Clave),
                RolId = rol.Id
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            
            if (dto.EsDoctor)
            {
                if (dto.EspecialidadIds == null || !dto.EspecialidadIds.Any())
                    throw new ArgumentException("Las especialidades son requeridas cuando se registra un doctor.");

                if (string.IsNullOrEmpty(dto.Apellido))
                    throw new ArgumentException("El apellido es requerido para doctores.");
                
                if (string.IsNullOrEmpty(dto.Documento))
                    throw new ArgumentException("El documento es requerido para doctores.");
                
                if (string.IsNullOrEmpty(dto.Telefono))
                    throw new ArgumentException("El teléfono es requerido para doctores.");
                
                if (string.IsNullOrEmpty(dto.Exequatur))
                    throw new ArgumentException("El exequatur es requerido para doctores.");

                var especialidades = await _context.Especialidades
                    .Where(e => dto.EspecialidadIds.Contains(e.Id)).ToListAsync();

                if (especialidades.Count != dto.EspecialidadIds.Count)
                    throw new ArgumentException("Una o más especialidades no son válidas.");

                var doctor = new Doctor
                {
                    Nombre = dto.Nombre,
                    Apellido = dto.Apellido,
                    Documento = dto.Documento,
                    Telefono = dto.Telefono,
                    Correo = dto.Correo,
                    Exequatur = dto.Exequatur,
                    UsuarioId = usuario.Id,
                    DoctorEspecialidades = especialidades.Select(e => new DoctorEspecialidad
                    {
                        EspecialidadId = e.Id
                    }).ToList()
                };

                _context.Doctores.Add(doctor);
                await _context.SaveChangesAsync();
            }
            return true;
        }
        public async Task<bool> UpdateUsuarioAsync(int id, UpdateUsuarioRequestDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            usuario.Nombre = dto.Nombre;
            usuario.Correo = dto.Correo;
            usuario.RolId = dto.RolId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CambiarEstadoUsuarioAsync(int id, bool estado)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            usuario.Status = estado;
            await _context.SaveChangesAsync();
            return true;
        }



    }
}
