namespace Seguimiento.Servicios.Catalogos;

// Hoy solo delega en el repositorio: no hay reglas sobre los catálogos. Existe para que
// el controlador dependa siempre de un servicio y nunca del acceso a datos.
public sealed class CatalogoService(ICatalogoRepositorio repositorio)
{
    public Task<Catalogos> ObtenerAsync(CancellationToken cancelacion) =>
        repositorio.ObtenerActivosAsync(cancelacion);
}
