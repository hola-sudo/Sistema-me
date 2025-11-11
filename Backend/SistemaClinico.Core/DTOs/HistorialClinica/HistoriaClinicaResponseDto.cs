using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaClinico.Core.DTOs.HistorialClinica
{
    public class HistoriaClinicaResponseDto
    {

        public int Id { get; set; }
        public int PacienteId { get; set; }
        public string PacienteNombre { get; set; } = string.Empty;
        public int MotivoConsultaId { get; set; }
        public string MotivoConsultaNombre { get; set; } = string.Empty;
        public int EspecialidadId { get; set; }
        public string EspecialidadNombre { get; set; } = string.Empty;
        public int DiagnosticoId { get; set; }
        public string DiagnosticoNombre { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public string DoctorNombre { get; set; } = string.Empty;
        public int UsuarioCreatedId { get; set; }
        public string UsuarioCreatedNombre { get; set; } = string.Empty;
        public int UsuarioModifiedId { get; set; }
        public string UsuarioModifiedNombre { get; set; } = string.Empty;
        public string Tratamiento { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    }
}