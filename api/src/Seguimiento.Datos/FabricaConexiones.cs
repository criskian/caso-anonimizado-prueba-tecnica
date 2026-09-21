using Microsoft.Data.SqlClient;

namespace Seguimiento.Datos;

/// <summary>Crea conexiones a SQL Server. Cada repositorio abre la suya y la cierra al terminar.</summary>
public sealed class FabricaConexiones(string cadenaConexion)
{
    public SqlConnection Crear() => new(cadenaConexion);
}
