using Dapper;
using Seguimiento.Servicios.Catalogos;
using Seguimiento.Servicios.Contactos;

namespace Seguimiento.Datos;

public sealed class ContactoRepositorio(FabricaConexiones fabrica) : IContactoRepositorio
{
    public async Task<PacienteParaContacto?> ObtenerPacienteAsync(int pacienteId, CancellationToken cancelacion)
    {
        const string sql = "SELECT Id, Estado, FechaIngresoPrograma FROM dbo.Paciente WHERE Id = @pacienteId;";
        await using var conexion = fabrica.Crear();
        var fila = await conexion.QuerySingleOrDefaultAsync<FilaPaciente>(
            new CommandDefinition(sql, new { pacienteId }, cancellationToken: cancelacion));
        return fila is null ? null : new PacienteParaContacto(fila.Id, fila.Estado, DateOnly.FromDateTime(fila.FechaIngresoPrograma));
    }

    public Task<bool?> ObtenerEstadoGestorAsync(int gestorId, CancellationToken cancelacion) =>
        ObtenerActivoAsync("SELECT Activo FROM dbo.Gestor WHERE Id = @valor;", gestorId, cancelacion);

    public Task<bool?> ObtenerEstadoCanalAsync(string codigo, CancellationToken cancelacion) =>
        ObtenerActivoAsync("SELECT Activo FROM dbo.CanalContacto WHERE Codigo = @valor;", codigo, cancelacion);

    public Task<bool?> ObtenerEstadoResultadoAsync(string codigo, CancellationToken cancelacion) =>
        ObtenerActivoAsync("SELECT Activo FROM dbo.ResultadoContacto WHERE Codigo = @valor;", codigo, cancelacion);

    public async Task<long> RegistrarAsync(ContactoARegistrar contacto, CancellationToken cancelacion)
    {
        // El contacto y su versión 1 se guardan en la misma transacción (H-01): o quedan los
        // dos o no queda ninguno. La versión copia los valores y la hora de registro de la
        // fila recién insertada, así proyección e historial coinciden desde el primer momento.
        const string sql = """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;

            INSERT INTO dbo.Contacto (PacienteId, GestorId, FechaContacto, CanalCodigo, ResultadoCodigo, Observacion)
            VALUES (@PacienteId, @GestorId, @FechaContacto, @Canal, @Resultado, @Observacion);

            DECLARE @Id BIGINT = SCOPE_IDENTITY();

            INSERT INTO dbo.ContactoVersion (ContactoId, NumeroVersion, FechaContacto, CanalCodigo, ResultadoCodigo,
                                             Observacion, MotivoCorreccion, RegistradoPorGestorId, RegistradoEnUtc)
            SELECT Id, 1, FechaContacto, CanalCodigo, ResultadoCodigo, Observacion, NULL, GestorId, CreadoEnUtc
            FROM dbo.Contacto
            WHERE Id = @Id;

            COMMIT TRANSACTION;

            SELECT @Id;
            """;

        await using var conexion = fabrica.Crear();
        return await conexion.ExecuteScalarAsync<long>(new CommandDefinition(sql, contacto, cancellationToken: cancelacion));
    }

    public async Task<ContactoDetalle?> ObtenerDetalleAsync(long contactoId, CancellationToken cancelacion)
    {
        const string sql = """
            SELECT c.Id, c.PacienteId, p.Nombre AS PacienteNombre, ci.Nombre AS Ciudad,
                   c.GestorId, g.Nombre AS GestorNombre, c.FechaContacto,
                   c.CanalCodigo, cc.Nombre AS CanalNombre, c.ResultadoCodigo, rc.Nombre AS ResultadoNombre,
                   c.Observacion, c.VersionActual
            FROM dbo.Contacto AS c
            JOIN dbo.Paciente AS p ON p.Id = c.PacienteId
            JOIN dbo.Ciudad AS ci ON ci.Id = p.CiudadId
            JOIN dbo.Gestor AS g ON g.Id = c.GestorId
            JOIN dbo.CanalContacto AS cc ON cc.Codigo = c.CanalCodigo
            JOIN dbo.ResultadoContacto AS rc ON rc.Codigo = c.ResultadoCodigo
            WHERE c.Id = @contactoId;

            SELECT v.NumeroVersion, v.FechaContacto,
                   v.CanalCodigo, cc.Nombre AS CanalNombre, v.ResultadoCodigo, rc.Nombre AS ResultadoNombre,
                   v.Observacion, v.MotivoCorreccion,
                   v.RegistradoPorGestorId, g.Nombre AS RegistradoPorNombre, v.RegistradoEnUtc
            FROM dbo.ContactoVersion AS v
            JOIN dbo.Gestor AS g ON g.Id = v.RegistradoPorGestorId
            JOIN dbo.CanalContacto AS cc ON cc.Codigo = v.CanalCodigo
            JOIN dbo.ResultadoContacto AS rc ON rc.Codigo = v.ResultadoCodigo
            WHERE v.ContactoId = @contactoId
            ORDER BY v.NumeroVersion;
            """;

        await using var conexion = fabrica.Crear();
        using var lector = await conexion.QueryMultipleAsync(new CommandDefinition(sql, new { contactoId }, cancellationToken: cancelacion));

        var c = await lector.ReadSingleOrDefaultAsync<FilaContacto>();
        if (c is null)
        {
            return null;
        }

        var historial = (await lector.ReadAsync<FilaVersion>())
            .Select(v => new VersionContacto(
                v.NumeroVersion,
                v.FechaContacto,
                new ItemCatalogo(v.CanalCodigo, v.CanalNombre),
                new ItemCatalogo(v.ResultadoCodigo, v.ResultadoNombre),
                v.Observacion,
                v.MotivoCorreccion,
                new GestorResumen(v.RegistradoPorGestorId, v.RegistradoPorNombre),
                // DATETIME2 llega sin zona: se marca como UTC para que se serialice con «Z».
                DateTime.SpecifyKind(v.RegistradoEnUtc, DateTimeKind.Utc)))
            .ToList();

        return new ContactoDetalle(
            c.Id,
            new PacienteDelContacto(c.PacienteId, c.PacienteNombre, c.Ciudad),
            new GestorResumen(c.GestorId, c.GestorNombre),
            c.FechaContacto,
            new ItemCatalogo(c.CanalCodigo, c.CanalNombre),
            new ItemCatalogo(c.ResultadoCodigo, c.ResultadoNombre),
            c.Observacion,
            c.VersionActual,
            historial);
    }

    private async Task<bool?> ObtenerActivoAsync(string sql, object valor, CancellationToken cancelacion)
    {
        await using var conexion = fabrica.Crear();
        return await conexion.QuerySingleOrDefaultAsync<bool?>(new CommandDefinition(sql, new { valor }, cancellationToken: cancelacion));
    }

    // Filas planas tal como las devuelve SQL; Dapper las llena por nombre y orden de columna.
    private sealed record FilaPaciente(int Id, string Estado, DateTime FechaIngresoPrograma);

    private sealed record FilaContacto(
        long Id, int PacienteId, string PacienteNombre, string Ciudad,
        int GestorId, string GestorNombre, DateTimeOffset FechaContacto,
        string CanalCodigo, string CanalNombre, string ResultadoCodigo, string ResultadoNombre,
        string? Observacion, int VersionActual);

    private sealed record FilaVersion(
        int NumeroVersion, DateTimeOffset FechaContacto,
        string CanalCodigo, string CanalNombre, string ResultadoCodigo, string ResultadoNombre,
        string? Observacion, string? MotivoCorreccion,
        int RegistradoPorGestorId, string RegistradoPorNombre, DateTime RegistradoEnUtc);
}
