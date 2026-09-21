using Dapper;
using Seguimiento.Servicios.Contactos;

namespace Seguimiento.Datos;

/// <summary>
/// Consulta de la vista del mes (CA-4). Combina contacto, paciente, ciudad, gestor y los dos
/// catálogos. La justificación de su forma y de sus índices está en 03-bitacora.md. Lee la proyección Contacto, que ya tiene los valores de la versión vigente, así
/// que una corrección se refleja sin buscar la última versión de cada contacto (CA-3).
/// </summary>
public sealed class ContactosDelMesRepositorio(FabricaConexiones fabrica) : IContactosDelMesRepositorio
{
    // Rango semiabierto sobre FechaContacto: puede resolverse con una búsqueda por rango en
    // IX_Contacto_FechaContacto (o en IX_Contacto_GestorId_FechaContacto si se filtra por gestor).
    // Los filtros son opcionales con «(@x IS NULL OR …)»; OPTION (RECOMPILE) hace que cada
    // ejecución se compile con los valores reales, así el optimizador descarta las ramas que no
    // aplican y elige el índice correcto, en vez de reutilizar un plan pensado para otra combinación.
    private const string Filtro = """
        FROM dbo.Contacto AS c
        JOIN dbo.Paciente AS p ON p.Id = c.PacienteId
        WHERE c.FechaContacto >= @Desde AND c.FechaContacto < @Hasta
          AND (@GestorId IS NULL OR c.GestorId = @GestorId)
          AND (@CiudadId IS NULL OR p.CiudadId = @CiudadId)
        """;

    private const string Sql = $"""
        SELECT COUNT(*)
        {Filtro}
        OPTION (RECOMPILE);

        -- Primero se filtra y se corta la página con solo las dos tablas que el filtro necesita;
        -- los nombres de ciudad, gestor, canal y resultado se buscan después, solo para esas filas.
        WITH Pagina AS (
            SELECT c.Id, c.FechaContacto, c.PacienteId, p.Nombre AS PacienteNombre, p.CiudadId,
                   c.GestorId, c.CanalCodigo, c.ResultadoCodigo, c.VersionActual
            {Filtro}
            ORDER BY c.FechaContacto DESC, c.Id DESC
            OFFSET (@Pagina - 1) * @Tamano ROWS FETCH NEXT @Tamano ROWS ONLY
        )
        SELECT pg.Id, pg.FechaContacto, pg.PacienteId, pg.PacienteNombre, pg.CiudadId, ci.Nombre AS Ciudad,
               pg.GestorId, g.Nombre AS GestorNombre,
               pg.CanalCodigo, cc.Nombre AS CanalNombre, pg.ResultadoCodigo, rc.Nombre AS ResultadoNombre,
               pg.VersionActual
        FROM Pagina AS pg
        JOIN dbo.Ciudad AS ci ON ci.Id = pg.CiudadId
        JOIN dbo.Gestor AS g ON g.Id = pg.GestorId
        JOIN dbo.CanalContacto AS cc ON cc.Codigo = pg.CanalCodigo
        JOIN dbo.ResultadoContacto AS rc ON rc.Codigo = pg.ResultadoCodigo
        -- Id desempata contactos con la misma fecha: sin él, el orden entre páginas no sería estable.
        ORDER BY pg.FechaContacto DESC, pg.Id DESC
        OPTION (RECOMPILE);
        """;

    public async Task<(IReadOnlyList<ContactoDelMes> Items, int Total)> ConsultarAsync(
        FiltroContactosDelMes filtro, CancellationToken cancelacion)
    {
        await using var conexion = fabrica.Crear();
        using var lector = await conexion.QueryMultipleAsync(new CommandDefinition(Sql, filtro, cancellationToken: cancelacion));

        var total = await lector.ReadSingleAsync<int>();
        var items = (await lector.ReadAsync<ContactoDelMes>()).AsList();
        return (items, total);
    }
}
