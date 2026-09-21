using Dapper;
using Seguimiento.Servicios.Catalogos;

namespace Seguimiento.Datos;

public sealed class CatalogoRepositorio(FabricaConexiones fabrica) : ICatalogoRepositorio
{
    // Las cuatro consultas viajan en un solo lote: un único viaje de ida y vuelta a la base.
    private const string Sql = """
        SELECT Id, Nombre FROM dbo.Gestor WHERE Activo = 1 ORDER BY Nombre;
        SELECT Id, Nombre, PaisCodigo FROM dbo.Ciudad ORDER BY PaisCodigo, Nombre;
        SELECT Codigo, Nombre FROM dbo.CanalContacto WHERE Activo = 1 ORDER BY Nombre;
        SELECT Codigo, Nombre FROM dbo.ResultadoContacto WHERE Activo = 1 ORDER BY Nombre;
        """;

    public async Task<Catalogos> ObtenerActivosAsync(CancellationToken cancelacion)
    {
        await using var conexion = fabrica.Crear();
        using var lector = await conexion.QueryMultipleAsync(new CommandDefinition(Sql, cancellationToken: cancelacion));

        var gestores = (await lector.ReadAsync<GestorResumen>()).AsList();
        var ciudades = (await lector.ReadAsync<CiudadResumen>()).AsList();
        var canales = (await lector.ReadAsync<ItemCatalogo>()).AsList();
        var resultados = (await lector.ReadAsync<ItemCatalogo>()).AsList();

        return new Catalogos(gestores, ciudades, canales, resultados);
    }
}
