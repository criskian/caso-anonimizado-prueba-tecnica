using Dapper;

namespace Seguimiento.Integracion.Tests;

/// <summary>CA-2 contra SQL Server real: lo que guarda la transacción de registro.</summary>
[Collection(ColeccionBaseDeDatos.Nombre)]
public sealed class CA2_RegistroIntegracionTests(BaseDeDatosFixture baseDeDatos)
{
    private readonly Escenario escenario = new(baseDeDatos);

    [Fact]
    public async Task CA2_RegistrarContacto_CreaVersionUnoSinMotivo()
    {
        var gestor = await escenario.NuevoGestorAsync();
        var paciente = await escenario.NuevoPacienteAsync(await escenario.NuevaCiudadAsync());
        var fecha = Escenario.Local(2025, 1, 15, 9, 30);

        var id = await escenario.RegistrarAsync(gestor, paciente, fecha, "NO_CONTESTA", "WHATSAPP");

        await using var conexion = escenario.Conexion();
        var fila = await conexion.QuerySingleAsync(
            """
            SELECT c.PacienteId, c.GestorId, c.FechaContacto, c.CanalCodigo, c.ResultadoCodigo, c.VersionActual, c.CreadoEnUtc,
                   v.NumeroVersion, v.FechaContacto AS FechaVersion, v.CanalCodigo AS CanalVersion,
                   v.ResultadoCodigo AS ResultadoVersion, v.MotivoCorreccion, v.RegistradoPorGestorId, v.RegistradoEnUtc
            FROM dbo.Contacto AS c
            JOIN dbo.ContactoVersion AS v ON v.ContactoId = c.Id
            WHERE c.Id = @id;
            """,
            new { id });

        Assert.Equal(paciente, (int)fila.PacienteId);
        Assert.Equal(gestor, (int)fila.GestorId);
        Assert.Equal(fecha, (DateTimeOffset)fila.FechaContacto);
        Assert.Equal("WHATSAPP", (string)fila.CanalCodigo);
        Assert.Equal("NO_CONTESTA", (string)fila.ResultadoCodigo);
        Assert.Equal(1, (int)fila.VersionActual);

        Assert.Equal(1, (int)fila.NumeroVersion);
        Assert.Null(fila.MotivoCorreccion);
        Assert.Equal(gestor, (int)fila.RegistradoPorGestorId);
        Assert.Equal(fecha, (DateTimeOffset)fila.FechaVersion);
        Assert.Equal("WHATSAPP", (string)fila.CanalVersion);
        Assert.Equal("NO_CONTESTA", (string)fila.ResultadoVersion);
        Assert.Equal((DateTime)fila.CreadoEnUtc, (DateTime)fila.RegistradoEnUtc);
    }
}
