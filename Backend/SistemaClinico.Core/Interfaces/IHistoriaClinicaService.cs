
using SistemaClinico.Core.DTOs.HistorialClinica;

namespace SistemaClinico.Core.Interfaces
{
    public interface IHistoriaClinicaService
    {
        Task<HistoriaClinicaResponseDto> GetHistoriaClinicaByIdAsync(int id);
        Task<IEnumerable<HistoriaClinicaResponseDto>> GetHistoriasClinicaByPacienteIdAsync(int pacienteId);
        Task<HistoriaClinicaResponseDto> CrearHistoriaClinicaAsync(HistoriaClinicaCreateDto historiaClinica);
        Task ActualizarHistoriaClinicaAsync(int id, HistoriaClinicaCreateDto historiaClinica);
        Task EliminarHistoriaClinicaAsync(int id);
    }
}