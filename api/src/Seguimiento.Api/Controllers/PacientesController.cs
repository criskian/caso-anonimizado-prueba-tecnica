using Microsoft.AspNetCore.Mvc;
using Seguimiento.Api.Dtos;
using Seguimiento.Servicios.Pacientes;

namespace Seguimiento.Api.Controllers;

[ApiController]
[Route("api/pacientes")]
public sealed class PacientesController(PacienteService servicio) : ControllerBase
{
    /// <summary>Hasta 20 pacientes activos cuyo nombre o documento contiene el texto (CA-2).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PacienteResumenDto>>> Buscar(
        [FromQuery] string? buscar, CancellationToken cancelacion)
    {
        var pacientes = await servicio.BuscarAsync(buscar, cancelacion);
        return Ok(pacientes.Select(PacienteResumenDto.Desde).ToList());
    }
}
