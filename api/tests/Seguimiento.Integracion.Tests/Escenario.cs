using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Time.Testing;
using Seguimiento.Datos;
using Seguimiento.Servicios.Contactos;

namespace Seguimiento.Integracion.Tests;

/// <summary>
/// Arma los datos de cada prueba sobre la base real: gestores, ciudades y pacientes nuevos,
/// y los servicios reales conectados a los repositorios reales.
/// </summary>
public sealed class Escenario(BaseDeDatosFixture baseDeDatos)
{
    // «Ahora» fijo para las reglas de fecha; los contactos de las pruebas son de 2025.
    public static readonly DateTimeOffset Ahora = new(2026, 9, 21, 15, 0, 0, TimeSpan.FromHours(-5));
    public static readonly TimeSpan UtcMenos5 = TimeSpan.FromHours(-5);

    private readonly FabricaConexiones fabrica = new(baseDeDatos.CadenaConexion);

    public ContactoService Contactos => new(new ContactoRepositorio(fabrica), new FakeTimeProvider(Ahora));

    public ContactosDelMesService ContactosDelMes => new(new ContactosDelMesRepositorio(fabrica), new FakeTimeProvider(Ahora));

    public ContactoRepositorio RepositorioContactos => new(fabrica);

    public SqlConnection Conexion() => fabrica.Crear();

    public async Task<int> NuevoGestorAsync()
    {
        var clave = Guid.NewGuid().ToString("N");
        await using var conexion = Conexion();
        return await conexion.ExecuteScalarAsync<int>(
            "INSERT INTO dbo.Gestor (Nombre, Email) OUTPUT INSERTED.Id VALUES (@nombre, @email);",
            new { nombre = $"Gestor {clave[..6]}", email = $"{clave}@prueba.com" });
    }

    public async Task<int> NuevaCiudadAsync()
    {
        await using var conexion = Conexion();
        return await conexion.ExecuteScalarAsync<int>(
            "INSERT INTO dbo.Ciudad (PaisCodigo, Nombre) OUTPUT INSERTED.Id VALUES ('CO', @nombre);",
            new { nombre = $"Ciudad {Guid.NewGuid():N}" });
    }

    public async Task<int> NuevoPacienteAsync(int ciudadId)
    {
        await using var conexion = Conexion();
        return await conexion.ExecuteScalarAsync<int>(
            """
            INSERT INTO dbo.Paciente (PaisCodigo, TipoDocumentoCodigo, NumeroDocumento, Nombre, Telefono, CiudadId,
                                      FechaInicioTratamiento, FechaIngresoPrograma, Estado)
            OUTPUT INSERTED.Id
            VALUES ('CO', 'CC', @documento, N'Paciente de prueba', '+573001234567', @ciudadId, '2020-01-01', '2020-01-01', 'ACTIVO');
            """,
            new { documento = Guid.NewGuid().ToString("N")[..20], ciudadId });
    }

    /// <summary>Registra un contacto con el servicio real y devuelve su Id.</summary>
    public async Task<long> RegistrarAsync(
        int gestorId, int pacienteId, DateTimeOffset fecha, string resultado = "EFECTIVO", string canal = "LLAMADA")
    {
        var detalle = await Contactos.RegistrarAsync(
            gestorId.ToString(), new NuevoContacto(pacienteId, fecha, canal, resultado, null), default);
        return detalle.Id;
    }

    public static DateTimeOffset Local(int anio, int mes, int dia, int hora = 10, int minuto = 0) =>
        new(anio, mes, dia, hora, minuto, 0, UtcMenos5);
}
