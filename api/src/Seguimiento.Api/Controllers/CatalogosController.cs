using Microsoft.AspNetCore.Mvc;
using Seguimiento.Api.Dtos;
using Seguimiento.Servicios.Catalogos;

namespace Seguimiento.Api.Controllers;

[ApiController]
[Route("api/catalogos")]
public sealed class CatalogosController(CatalogoService servicio) : ControllerBase
{
    /// <summary>Gestores, ciudades, canales y resultados activos, para listas y filtros.</summary>
    [HttpGet]
    public async Task<ActionResult<CatalogosDto>> Obtener(CancellationToken cancelacion) =>
        Ok(CatalogosDto.Desde(await servicio.ObtenerAsync(cancelacion)));
}
