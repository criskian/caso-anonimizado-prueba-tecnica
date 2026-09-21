using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Seguimiento.Integracion.Tests;

/// <summary>
/// Crea una base de datos real para las pruebas aplicando los mismos scripts de scripts/
/// (000 a 006, sin datos de prueba). Por defecto levanta SQL Server en un contenedor con
/// Testcontainers; si existe la variable SEGUIMIENTO_PRUEBAS_SERVIDOR (cadena de conexión a
/// un servidor, por ejemplo el de docker compose), usa ese servidor. En ambos casos la base
/// tiene un nombre único y se borra al terminar, así nunca toca la base de desarrollo.
/// </summary>
public sealed partial class BaseDeDatosFixture : IAsyncLifetime
{
    public const string VariableServidor = "SEGUIMIENTO_PRUEBAS_SERVIDOR";

    private MsSqlContainer? contenedor;
    private string cadenaServidor = string.Empty;
    private readonly string nombreBase = $"SeguimientoPruebas_{Guid.NewGuid():N}"[..28];

    public string CadenaConexion { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var servidorExterno = Environment.GetEnvironmentVariable(VariableServidor);
        if (string.IsNullOrWhiteSpace(servidorExterno))
        {
            // La misma imagen que docker-compose.yml, así no se descarga otra.
            contenedor = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await contenedor.StartAsync();
            cadenaServidor = contenedor.GetConnectionString();
        }
        else
        {
            cadenaServidor = servidorExterno;
        }

        CadenaConexion = new SqlConnectionStringBuilder(cadenaServidor) { InitialCatalog = nombreBase }.ConnectionString;

        var carpetaScripts = BuscarCarpetaScripts();
        var scripts = Directory.GetFiles(carpetaScripts, "0*.sql").Order(StringComparer.Ordinal).ToList();

        await using var conexion = new SqlConnection(cadenaServidor);
        await conexion.OpenAsync();
        foreach (var script in scripts)
        {
            // Los scripts se aplican tal cual; solo en el 000, que crea la base, se cambia su nombre.
            var texto = await File.ReadAllTextAsync(script);
            if (Path.GetFileName(script).StartsWith("000_", StringComparison.Ordinal))
            {
                texto = NombreBaseDesarrollo().Replace(texto, nombreBase);
            }
            else
            {
                conexion.ChangeDatabase(nombreBase);
            }

            foreach (var lote in SeparadorGo().Split(texto).Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                await using var comando = new SqlCommand(lote, conexion);
                await comando.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (contenedor is not null)
        {
            await contenedor.DisposeAsync();
            return;
        }

        SqlConnection.ClearAllPools();
        await using var conexion = new SqlConnection(cadenaServidor);
        await conexion.OpenAsync();
        await using var comando = new SqlCommand(
            $"ALTER DATABASE [{nombreBase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{nombreBase}];", conexion);
        await comando.ExecuteNonQueryAsync();
    }

    private static string BuscarCarpetaScripts()
    {
        for (var carpeta = new DirectoryInfo(AppContext.BaseDirectory); carpeta is not null; carpeta = carpeta.Parent)
        {
            var candidata = Path.Combine(carpeta.FullName, "scripts");
            if (File.Exists(Path.Combine(candidata, "000_base_de_datos.sql")))
            {
                return candidata;
            }
        }

        throw new DirectoryNotFoundException("No se encontró la carpeta scripts/ en la raíz del repositorio.");
    }

    // «GO» no es T-SQL: es el separador de lotes de sqlcmd, así que aquí se separa a mano.
    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorGo();

    [GeneratedRegex(@"\bSeguimiento\b")]
    private static partial Regex NombreBaseDesarrollo();
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionBaseDeDatos : ICollectionFixture<BaseDeDatosFixture>
{
    // Todas las clases de prueba comparten un solo servidor. Cada prueba crea sus propios
    // gestores, ciudades y pacientes y usa su propio mes, así no dependen unas de otras.
    public const string Nombre = "BaseDeDatos";
}
