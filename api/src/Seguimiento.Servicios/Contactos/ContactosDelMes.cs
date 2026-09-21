namespace Seguimiento.Servicios.Contactos;

/// <summary>
/// Filtro ya validado de la vista del mes. El rango es semiabierto [Desde, Hasta) y está
/// expresado en hora local del programa, así el mes se decide por la fecha del contacto en UTC−5.
/// </summary>
public sealed record FiltroContactosDelMes(
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    int? GestorId,
    int? CiudadId,
    int Pagina,
    int Tamano);

/// <summary>Fila de la vista del mes: siempre con los valores de la versión vigente.</summary>
public sealed record ContactoDelMes(
    long Id,
    DateTimeOffset FechaContacto,
    int PacienteId,
    string PacienteNombre,
    int CiudadId,
    string Ciudad,
    int GestorId,
    string GestorNombre,
    string CanalCodigo,
    string CanalNombre,
    string ResultadoCodigo,
    string ResultadoNombre,
    int VersionActual);

public sealed record PaginaContactosDelMes(
    string Mes,
    int Pagina,
    int Tamano,
    int Total,
    IReadOnlyList<ContactoDelMes> Items);

public interface IContactosDelMesRepositorio
{
    /// <summary>Devuelve la página pedida y el total de contactos que cumplen el filtro.</summary>
    Task<(IReadOnlyList<ContactoDelMes> Items, int Total)> ConsultarAsync(
        FiltroContactosDelMes filtro, CancellationToken cancelacion);
}
