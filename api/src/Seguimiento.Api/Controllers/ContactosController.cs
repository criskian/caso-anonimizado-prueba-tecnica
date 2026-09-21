using Microsoft.AspNetCore.Mvc;
using Seguimiento.Api.Dtos;
using Seguimiento.Servicios.Contactos;

namespace Seguimiento.Api.Controllers;

[ApiController]
[Route("api/contactos")]
public sealed class ContactosController(ContactoService servicio) : ControllerBase
{
    // Identidad simulada (H-11): la interfaz envía el gestor elegido en «Actuando como».
    public const string CabeceraGestor = "X-Gestor-Id";

    /// <summary>CA-2: registra un contacto y devuelve su detalle con la versión 1.</summary>
    [HttpPost]
    public async Task<ActionResult<ContactoDetalleDto>> Registrar(
        [FromHeader(Name = CabeceraGestor)] string? gestorId,
        [FromBody] RegistrarContactoDto cuerpo,
        CancellationToken cancelacion)
    {
        var detalle = await servicio.RegistrarAsync(gestorId, cuerpo.ANuevoContacto(), cancelacion);
        return CreatedAtAction(nameof(Obtener), new { id = detalle.Id }, ContactoDetalleDto.Desde(detalle));
    }

    /// <summary>
    /// CA-3: corrige un contacto creando una versión nueva. Es un POST a un subrecurso y no
    /// un PUT porque no reemplaza el contacto: agrega una corrección a su historial.
    /// </summary>
    [HttpPost("{id:long}/correcciones")]
    public async Task<ActionResult<ContactoDetalleDto>> Corregir(
        long id,
        [FromHeader(Name = CabeceraGestor)] string? gestorId,
        [FromBody] CorregirContactoDto cuerpo,
        CancellationToken cancelacion)
    {
        var detalle = await servicio.CorregirAsync(gestorId, id, cuerpo.ACorreccion(), cancelacion);
        return Ok(ContactoDetalleDto.Desde(detalle));
    }

    /// <summary>Valores vigentes del contacto y su historial de versiones.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ContactoDetalleDto>> Obtener(long id, CancellationToken cancelacion) =>
        Ok(ContactoDetalleDto.Desde(await servicio.ObtenerAsync(id, cancelacion)));
}
