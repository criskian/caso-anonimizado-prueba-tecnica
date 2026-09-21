namespace Seguimiento.Servicios.Catalogos;

public sealed record GestorResumen(int Id, string Nombre);

public sealed record CiudadResumen(int Id, string Nombre, string PaisCodigo);

public sealed record ItemCatalogo(string Codigo, string Nombre);

/// <summary>Valores activos de los catálogos que usa la interfaz para sus listas y filtros.</summary>
public sealed record Catalogos(
    IReadOnlyList<GestorResumen> Gestores,
    IReadOnlyList<CiudadResumen> Ciudades,
    IReadOnlyList<ItemCatalogo> Canales,
    IReadOnlyList<ItemCatalogo> Resultados);
