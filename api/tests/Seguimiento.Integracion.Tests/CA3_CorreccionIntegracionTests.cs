using Dapper;
using Microsoft.Data.SqlClient;
using Seguimiento.Servicios.Contactos;

namespace Seguimiento.Integracion.Tests;

/// <summary>
/// CA-3 contra SQL Server real: la vista del mes refleja la corrección, la base no deja
/// reescribir el historial y dos correcciones simultáneas no pisan una a la otra.
/// </summary>
[Collection(ColeccionBaseDeDatos.Nombre)]
public sealed class CA3_CorreccionIntegracionTests(BaseDeDatosFixture baseDeDatos)
{
    private const string Motivo = "El paciente sí contestó; se registró mal el resultado.";
    private readonly Escenario escenario = new(baseDeDatos);

    private async Task<(int Gestor, long ContactoId, DateTimeOffset Fecha)> ContactoNuevoAsync(int mes)
    {
        var gestor = await escenario.NuevoGestorAsync();
        var paciente = await escenario.NuevoPacienteAsync(await escenario.NuevaCiudadAsync());
        var fecha = Escenario.Local(2025, mes, 10);
        return (gestor, await escenario.RegistrarAsync(gestor, paciente, fecha, "NO_CONTESTA"), fecha);
    }

    [Fact]
    public async Task CA3_VistaDelMes_MuestraLosValoresCorregidos()
    {
        var (gestor, id, fecha) = await ContactoNuevoAsync(mes: 2);

        await escenario.Contactos.CorregirAsync(
            gestor.ToString(), id, new CorreccionContacto(1, fecha, "LLAMADA", "EFECTIVO", null, Motivo), default);

        var pagina = await escenario.ContactosDelMes.ConsultarAsync("2025-02", gestor, null, null, null, default);
        var fila = Assert.Single(pagina.Items);
        Assert.Equal(id, fila.Id);
        Assert.Equal("EFECTIVO", fila.ResultadoCodigo);
        Assert.Equal(2, fila.VersionActual);
    }

    [Fact]
    public async Task CA3_ProyeccionCoincideConLaVersionVigente()
    {
        var (gestor, id, fecha) = await ContactoNuevoAsync(mes: 2);
        var contactos = escenario.Contactos;
        await contactos.CorregirAsync(gestor.ToString(), id, new CorreccionContacto(1, fecha, "LLAMADA", "EFECTIVO", null, Motivo), default);
        await contactos.CorregirAsync(gestor.ToString(), id, new CorreccionContacto(2, fecha.AddHours(1), "CORREO", "EFECTIVO", "Respondió por correo", Motivo), default);

        await using var conexion = escenario.Conexion();
        var diferencias = await conexion.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM dbo.Contacto AS c
            JOIN dbo.ContactoVersion AS v ON v.ContactoId = c.Id AND v.NumeroVersion = c.VersionActual
            WHERE c.Id = @id
              AND (c.VersionActual <> 3 OR v.FechaContacto <> c.FechaContacto OR v.CanalCodigo <> c.CanalCodigo
                   OR v.ResultadoCodigo <> c.ResultadoCodigo OR ISNULL(v.Observacion, N'') <> ISNULL(c.Observacion, N''));
            """,
            new { id });
        var versiones = await conexion.QueryAsync<(int Numero, string Resultado)>(
            "SELECT NumeroVersion, ResultadoCodigo FROM dbo.ContactoVersion WHERE ContactoId = @id ORDER BY NumeroVersion;", new { id });

        Assert.Equal(0, diferencias);
        Assert.Equal([(1, "NO_CONTESTA"), (2, "EFECTIVO"), (3, "EFECTIVO")], versiones);
    }

    [Theory]
    [InlineData("UPDATE dbo.ContactoVersion SET ResultadoCodigo = 'EFECTIVO' WHERE ContactoId = @id;", 51001)]
    [InlineData("DELETE dbo.ContactoVersion WHERE ContactoId = @id;", 51001)]
    [InlineData("DELETE dbo.Contacto WHERE Id = @id;", 51002)]
    [InlineData("UPDATE dbo.Contacto SET GestorId = GestorId + 0, PacienteId = (SELECT MIN(Id) FROM dbo.Paciente WHERE Id <> PacienteId) WHERE Id = @id;", 51003)]
    public async Task CA3_BaseDeDatos_RechazaReescribirElHistorial(string sql, int errorEsperado)
    {
        var (_, id, _) = await ContactoNuevoAsync(mes: 2);

        await using var conexion = escenario.Conexion();
        var error = await Assert.ThrowsAsync<SqlException>(() => conexion.ExecuteAsync(sql, new { id }));

        Assert.Equal(errorEsperado, error.Number);
        var versiones = await conexion.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.ContactoVersion WHERE ContactoId = @id;", new { id });
        Assert.Equal(1, versiones);
    }

    [Fact]
    public async Task CA3_CorreccionesSimultaneas_SoloUnaSeGuarda()
    {
        var (gestor, id, fecha) = await ContactoNuevoAsync(mes: 2);
        var repositorio = escenario.RepositorioContactos;

        // Diez correcciones hechas sobre la misma versión 1, lanzadas a la vez.
        var resultados = await Task.WhenAll(Enumerable.Range(1, 10).Select(i => repositorio.CorregirAsync(
            new CorreccionARegistrar(id, 1, gestor, fecha, "LLAMADA", "EFECTIVO", $"intento {i}", Motivo), default)));

        Assert.Equal(1, resultados.Count(guardada => guardada));
        await using var conexion = escenario.Conexion();
        Assert.Equal(2, await conexion.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.ContactoVersion WHERE ContactoId = @id;", new { id }));
    }
}
