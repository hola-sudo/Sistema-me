using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaClinico.Core.DTOs.HistorialClinica
{
    public class HistoriaClinicaCreateDto
    {
        public int PacienteId { get; set; }
        public int DoctorId { get; set; }
        public int MotivoConsultaId { get; set; }
        public int EspecialidadId { get; set; }
        public int DiagnosticoId { get; set; }
        public int UsuarioCreatedId { get; set; }
        public int UsuarioModifiedId { get; set; }
        public string Tratamiento { get; set; } = string.Empty;
    }
}