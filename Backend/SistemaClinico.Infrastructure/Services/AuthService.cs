using System;
using System.Collections.Generic;
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
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
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
            try
            {
                var exists = await _context.Usuarios.AnyAsync(u => u.Correo == dto.Correo);
                if (exists) return false;

                // Buscar el rol por nombre
                var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id == dto.RolId);
                if (rol == null)
                    throw new KeyNotFoundException("El rol especificado no existe.");

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
                    var especialidades = await _context.Especialidades
                        .Where(e => dto.EspecialidadIds.Contains(e.Id)).ToListAsync();

                    var doctor = new Doctor
                    {
                        Nombre = dto.Nombre,
                        Apellido = dto.Apellido!,
                        Documento = dto.Documento!,
                        Telefono = dto.Telefono!,
                        Correo = dto.Correo,
                        Exequatur = dto.Exequatur!,
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
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error registrando usuario: {ex.Message}");
                throw;
            }

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
