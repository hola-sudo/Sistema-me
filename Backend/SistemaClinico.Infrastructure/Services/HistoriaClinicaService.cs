using Microsoft.EntityFrameworkCore;
using SistemaClinico.Core.DTOs.HistorialClinica;
using SistemaClinico.Core.Entities;
using SistemaClinico.Core.Interfaces;
using SistemaClinico.Infrastructure.Data;

namespace SistemaClinico.Infrastructure.Services
{
    public class HistoriaClinicaService : IHistoriaClinicaService
    {
        private readonly AppDbContext _context;

        public HistoriaClinicaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HistoriaClinicaResponseDto> GetHistoriaClinicaByIdAsync(int id)
        {
            var historiaClinica = await _context.HistoriasClinicas
                .Include(h => h.Doctor)
                .Include(h => h.MotivoConsulta)
                .Include(h => h.Diagnostico)
                .Include(h => h.Especialidad)
                .Include(h => h.UsuarioCreated)
                .Include(h => h.UsuarioModified)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (historiaClinica == null)
                throw new KeyNotFoundException($"Historia Clínica con ID {id} no encontrada.");

            return new HistoriaClinicaResponseDto
            {
                Id = historiaClinica.Id,
                PacienteId = historiaClinica.PacienteId,
                FechaRegistro = historiaClinica.FechaRegistro,
                DoctorId = historiaClinica.DoctorId,
                DoctorNombre = historiaClinica.Doctor?.Nombre ?? "",
                MotivoConsultaId = historiaClinica.MotivoConsultaId,
                MotivoConsultaNombre = historiaClinica.MotivoConsulta?.Nombre ?? "",
                EspecialidadId = historiaClinica.EspecialidadId,
                EspecialidadNombre = historiaClinica.Especialidad?.Nombre ?? "",
                DiagnosticoId = historiaClinica.DiagnosticoId,
                DiagnosticoNombre = historiaClinica.Diagnostico?.Nombre ?? "",
                UsuarioCreatedId = historiaClinica.UsuarioCreatedId,
                UsuarioCreatedNombre = historiaClinica.UsuarioCreated?.Nombre ?? "",
                UsuarioModifiedId = historiaClinica.UsuarioModifiedId,
                UsuarioModifiedNombre = historiaClinica.UsuarioModified?.Nombre ?? "",
                Tratamiento = historiaClinica.Tratamiento
            };
        }

        public async Task<IEnumerable<HistoriaClinicaResponseDto>> GetHistoriasClinicaByPacienteIdAsync(int pacienteId)
        {
            var historias = await _context.HistoriasClinicas
                .Where(h => h.PacienteId == pacienteId)
                .Include(h => h.Doctor)
                .Include(h => h.MotivoConsulta)
                .Include(h => h.Diagnostico)
                .Include(h => h.Especialidad)
                .Include(h => h.UsuarioCreated)
                .Include(h => h.UsuarioModified)
                .ToListAsync();

            return historias.Select(h => new HistoriaClinicaResponseDto
            {
                Id = h.Id,
                PacienteId = h.PacienteId,
                FechaRegistro = h.FechaRegistro,
                DoctorId = h.DoctorId,
                DoctorNombre = h.Doctor?.Nombre ?? "",
                MotivoConsultaId = h.MotivoConsultaId,
                MotivoConsultaNombre = h.MotivoConsulta?.Nombre ?? "",
                EspecialidadId = h.EspecialidadId,
                EspecialidadNombre = h.Especialidad?.Nombre ?? "",
                DiagnosticoId = h.DiagnosticoId,
                DiagnosticoNombre = h.Diagnostico?.Nombre ?? "",
                UsuarioCreatedId = h.UsuarioCreatedId,
                UsuarioCreatedNombre = h.UsuarioCreated?.Nombre ?? "",
                UsuarioModifiedId = h.UsuarioModifiedId,
                UsuarioModifiedNombre = h.UsuarioModified?.Nombre ?? "",
                Tratamiento = h.Tratamiento
            });
        }

        public async Task<HistoriaClinicaResponseDto> CrearHistoriaClinicaAsync(HistoriaClinicaCreateDto dto)
        {
            // Validaciones
            if (!await _context.Pacientes.AnyAsync(p => p.Id == dto.PacienteId))
                throw new Exception("El paciente no existe.");

            if (!await _context.Doctores.AnyAsync(d => d.Id == dto.DoctorId))
                throw new Exception("El doctor no existe.");

            if (!await _context.MotivosConsulta.AnyAsync(m => m.Id == dto.MotivoConsultaId))
                throw new Exception("El motivo de consulta no existe.");

            if (!await _context.Especialidades.AnyAsync(e => e.Id == dto.EspecialidadId))
                throw new Exception("La especialidad no existe.");

            if (!await _context.Diagnosticos.AnyAsync(d => d.Id == dto.DiagnosticoId))
                throw new Exception("El diagnóstico no existe.");

            if (!await _context.Usuarios.AnyAsync(u => u.Id == dto.UsuarioCreatedId))
                throw new Exception("El usuario que crea no existe.");

            if (!await _context.Usuarios.AnyAsync(u => u.Id == dto.UsuarioModifiedId))
                throw new Exception("El usuario que modifica no existe.");

            var nueva = new HistoriaClinica
            {
                PacienteId = dto.PacienteId,
                DoctorId = dto.DoctorId,
                MotivoConsultaId = dto.MotivoConsultaId,
                EspecialidadId = dto.EspecialidadId,
                DiagnosticoId = dto.DiagnosticoId,
                UsuarioCreatedId = dto.UsuarioCreatedId,
                UsuarioModifiedId = dto.UsuarioModifiedId,
                Tratamiento = dto.Tratamiento,
                FechaRegistro = DateTime.UtcNow
            };

            _context.HistoriasClinicas.Add(nueva);
            await _context.SaveChangesAsync();

            await _context.Entry(nueva).Reference(h => h.Doctor).LoadAsync();
            await _context.Entry(nueva).Reference(h => h.MotivoConsulta).LoadAsync();
            await _context.Entry(nueva).Reference(h => h.Diagnostico).LoadAsync();
            await _context.Entry(nueva).Reference(h => h.Especialidad).LoadAsync();
            await _context.Entry(nueva).Reference(h => h.UsuarioCreated).LoadAsync();
            await _context.Entry(nueva).Reference(h => h.UsuarioModified).LoadAsync();

            return new HistoriaClinicaResponseDto
            {
                Id = nueva.Id,
                PacienteId = nueva.PacienteId,
                FechaRegistro = nueva.FechaRegistro,
                DoctorId = nueva.DoctorId,
                DoctorNombre = nueva.Doctor?.Nombre ?? "",
                MotivoConsultaId = nueva.MotivoConsultaId,
                MotivoConsultaNombre = nueva.MotivoConsulta?.Nombre ?? "",
                EspecialidadId = nueva.EspecialidadId,
                EspecialidadNombre = nueva.Especialidad?.Nombre ?? "",
                DiagnosticoId = nueva.DiagnosticoId,
                DiagnosticoNombre = nueva.Diagnostico?.Nombre ?? "",
                UsuarioCreatedId = nueva.UsuarioCreatedId,
                UsuarioCreatedNombre = nueva.UsuarioCreated?.Nombre ?? "",
                UsuarioModifiedId = nueva.UsuarioModifiedId,
                UsuarioModifiedNombre = nueva.UsuarioModified?.Nombre ?? "",
                Tratamiento = nueva.Tratamiento
            };
        }

        public async Task ActualizarHistoriaClinicaAsync(int id, HistoriaClinicaCreateDto dto)
        {
            var historia = await _context.HistoriasClinicas.FindAsync(id);
            if (historia == null)
                throw new KeyNotFoundException($"Historia Clínica con ID {id} no encontrada.");

            if (!await _context.Pacientes.AnyAsync(p => p.Id == dto.PacienteId))
                throw new Exception("El paciente no existe.");

            if (!await _context.Doctores.AnyAsync(d => d.Id == dto.DoctorId))
                throw new Exception("El doctor no existe.");

            if (!await _context.MotivosConsulta.AnyAsync(m => m.Id == dto.MotivoConsultaId))
                throw new Exception("El motivo de consulta no existe.");

            if (!await _context.Especialidades.AnyAsync(e => e.Id == dto.EspecialidadId))
                throw new Exception("La especialidad no existe.");

            if (!await _context.Diagnosticos.AnyAsync(d => d.Id == dto.DiagnosticoId))
                throw new Exception("El diagnóstico no existe.");

            if (!await _context.Usuarios.AnyAsync(u => u.Id == dto.UsuarioModifiedId))
                throw new Exception("El usuario que modifica no existe.");

            historia.PacienteId = dto.PacienteId;
            historia.DoctorId = dto.DoctorId;
            historia.MotivoConsultaId = dto.MotivoConsultaId;
            historia.EspecialidadId = dto.EspecialidadId;
            historia.DiagnosticoId = dto.DiagnosticoId;
            historia.UsuarioModifiedId = dto.UsuarioModifiedId;
            historia.Tratamiento = dto.Tratamiento;

            _context.HistoriasClinicas.Update(historia);
            await _context.SaveChangesAsync();
        }

        public async Task EliminarHistoriaClinicaAsync(int id)
        {
            var historia = await _context.HistoriasClinicas.FindAsync(id);
            if (historia == null)
                throw new KeyNotFoundException($"Historia Clínica con ID {id} no encontrada.");

            _context.HistoriasClinicas.Remove(historia);
            await _context.SaveChangesAsync();
        }
    }
}
