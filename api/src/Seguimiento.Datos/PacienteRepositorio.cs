using Dapper;
using Seguimiento.Servicios.Pacientes;

namespace Seguimiento.Datos;

public sealed class PacienteRepositorio(FabricaConexiones fabrica) : IPacienteRepositorio
{
    public async Task<IReadOnlyList<PacienteResumen>> BuscarActivosAsync(string texto, int maximo, CancellationToken cancelacion)
    {
        // «Contiene» obliga a recorrer la tabla (el comodín inicial impide usar un índice).
        // Con miles de pacientes activos es aceptable; con cientos de miles haría falta
        // búsqueda de texto completo.
        const string sql = """
            SELECT TOP (@maximo) p.Id, p.Nombre, p.TipoDocumentoCodigo AS TipoDocumento, p.NumeroDocumento, c.Nombre AS Ciudad
            FROM dbo.Paciente AS p
            JOIN dbo.Ciudad AS c ON c.Id = p.CiudadId
            WHERE p.Estado = 'ACTIVO'
              AND (p.Nombre LIKE @patron ESCAPE '\' OR p.NumeroDocumento LIKE @patron ESCAPE '\')
            ORDER BY p.Nombre;
            """;

        // Los comodines que escriba el usuario (%, _, [) se buscan como texto literal.
        var patron = "%" + texto.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_").Replace("[", @"\[") + "%";

        await using var conexion = fabrica.Crear();
        var filas = await conexion.QueryAsync<PacienteResumen>(
            new CommandDefinition(sql, new { maximo, patron }, cancellationToken: cancelacion));
        return filas.AsList();
    }
}
