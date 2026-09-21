namespace Seguimiento.Servicios.Catalogos;

public interface ICatalogoRepositorio
{
    /// <summary>Devuelve solo los valores activos de cada catálogo, ordenados por nombre.</summary>
    Task<Catalogos> ObtenerActivosAsync(CancellationToken cancelacion);
}
