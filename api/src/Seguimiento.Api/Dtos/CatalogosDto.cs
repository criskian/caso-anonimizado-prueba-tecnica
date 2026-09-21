using Seguimiento.Servicios.Catalogos;

namespace Seguimiento.Api.Dtos;

// Objetos de transferencia de GET /api/catalogos. Son propios de la API: si el modelo
// del servicio cambia, el contrato con la interfaz no cambia sin que lo decidamos aquí.

public sealed record GestorDto(int Id, string Nombre);

public sealed record CiudadDto(int Id, string Nombre, string PaisCodigo);

public sealed record ItemCatalogoDto(string Codigo, string Nombre);

public sealed record CatalogosDto(
    IReadOnlyList<GestorDto> Gestores,
    IReadOnlyList<CiudadDto> Ciudades,
    IReadOnlyList<ItemCatalogoDto> Canales,
    IReadOnlyList<ItemCatalogoDto> Resultados)
{
    public static CatalogosDto Desde(Catalogos catalogos) => new(
        catalogos.Gestores.Select(g => new GestorDto(g.Id, g.Nombre)).ToList(),
        catalogos.Ciudades.Select(c => new CiudadDto(c.Id, c.Nombre, c.PaisCodigo)).ToList(),
        catalogos.Canales.Select(c => new ItemCatalogoDto(c.Codigo, c.Nombre)).ToList(),
        catalogos.Resultados.Select(r => new ItemCatalogoDto(r.Codigo, r.Nombre)).ToList());
}
