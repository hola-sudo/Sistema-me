using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaClinico.Core.DTOs.Auth;
using SistemaClinico.Core.Interfaces;
using SistemaClinico.Core.Response;

namespace SistemaClinico.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    //Realizar un endpoint para asignar los roles y permisos
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var token = await _authService.LoginAsync(dto);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error en login: " + ex.Message);
            return Unauthorized(new ApiResponse<object>(401, "Correo o contraseña incorrectos."));
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result)
            {
                return Conflict(new ApiResponse<object>(409, "El correo ya está registrado o el rol no existe."));
            }

            return Ok(new ApiResponse<object>(200, "Usuario creado exitosamente."));

        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            if (innerMessage.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new ApiResponse<object>(409, "El correo ya está registrado o el rol no existe."));
            }
            else
            {
                return BadRequest(new ApiResponse<object>(400, "Problema registrando el usuario"));
            }
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>(404, ex.Message));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error inesperado al registrar usuario: {ex.Message}");
            return StatusCode(500, new ApiResponse<object>(500, "Error interno al registrar el usuario."));
        }
    }
    [Authorize]
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new ApiResponse<object>(401, "Usuario no autorizado o token inválido."));

            var token = await _authService.GenerateTokenByUserIdAsync(userId);

            return Ok(new { token });
        }
        catch
        {
            return Unauthorized(new ApiResponse<object>(401, "Usuario no autorizado o token inválido."));
        }
    }
    //[Authorize]
    [HttpGet("users")]
    public async Task<IActionResult> GetUsuarios()
    {
        var usuarios = await _authService.GetUsuariosAsync();
        return Ok(new ApiResponse<IEnumerable<UsuarioDto>>(usuarios, "Usuarios obtenidos correctamente."));
    }
    [HttpPut("users/{id}")]
    public async Task<IActionResult> ActualizarUsuario(int id, [FromBody] UpdateUsuarioRequestDto dto)
    {
        var actualizado = await _authService.UpdateUsuarioAsync(id, dto);
        if (!actualizado)
            return NotFound(new ApiResponse<object>(404, "Usuario no encontrado"));

        return Ok(new ApiResponse<object>(200, "Usuario actualizado correctamente"));
    }

    [HttpPatch("users/{id}/estado")]
    public async Task<IActionResult> CambiarEstadoUsuario(int id, [FromBody] CambiarEstadoUsuarioDto dto)
    {
        var actualizado = await _authService.CambiarEstadoUsuarioAsync(id, dto.EstaActivo);
        if (!actualizado)
            return NotFound(new ApiResponse<object>(404, "Usuario no encontrado"));

        var estadoTexto = dto.EstaActivo ? "habilitado" : "inhabilitado";
        return Ok(new ApiResponse<object>(200, $"Usuario {estadoTexto} correctamente"));
    }
}
