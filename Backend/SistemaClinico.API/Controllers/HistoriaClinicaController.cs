using Microsoft.AspNetCore.Mvc;
using SistemaClinico.Core.DTOs.HistorialClinica;
using SistemaClinico.Core.Interfaces;

namespace SistemaClinico.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HistoriaClinica : ControllerBase
    {
        private readonly IHistoriaClinicaService _historiaClinicaService;
        public HistoriaClinica(IHistoriaClinicaService historiaClinicaService)
        {
            _historiaClinicaService = historiaClinicaService;
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetHistoriaClinicaById(int id)
        {
            var historiaClinica = await _historiaClinicaService.GetHistoriaClinicaByIdAsync(id);
            if (historiaClinica == null)
            {
                return NotFound();
            }
            return Ok(historiaClinica);
        }

        [HttpGet("paciente/{pacienteId}")]
        public async Task<IActionResult> GetHistoriasClinicaByPacienteId(int pacienteId)
        {
            var historiasClinicas = await _historiaClinicaService.GetHistoriasClinicaByPacienteIdAsync(pacienteId);
            return Ok(historiasClinicas);
        }
        [HttpPost]
        public async Task<IActionResult> CrearHistoriaClinica([FromBody] HistoriaClinicaCreateDto historiaClinica)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var creada = await _historiaClinicaService.CrearHistoriaClinicaAsync(historiaClinica);
            return CreatedAtAction(nameof(GetHistoriaClinicaById), new { id = creada.Id }, creada);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarHistoriaClinica(int id, [FromBody] HistoriaClinicaCreateDto historiaClinica)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _historiaClinicaService.ActualizarHistoriaClinicaAsync(id, historiaClinica);
            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarHistoriaClinica(int id)
        {
            await _historiaClinicaService.EliminarHistoriaClinicaAsync(id);
            return NoContent();
        } 
    }
}