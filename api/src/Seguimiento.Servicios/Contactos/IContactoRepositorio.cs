namespace Seguimiento.Servicios.Contactos;

public interface IContactoRepositorio
{
    Task<PacienteParaContacto?> ObtenerPacienteAsync(int pacienteId, CancellationToken cancelacion);

    /// <summary>null si el gestor no existe; si existe, indica si está activo.</summary>
    Task<bool?> ObtenerEstadoGestorAsync(int gestorId, CancellationToken cancelacion);

    /// <summary>null si el canal no existe; si existe, indica si está activo.</summary>
    Task<bool?> ObtenerEstadoCanalAsync(string codigo, CancellationToken cancelacion);

    /// <summary>null si el resultado no existe; si existe, indica si está activo.</summary>
    Task<bool?> ObtenerEstadoResultadoAsync(string codigo, CancellationToken cancelacion);

    /// <summary>Guarda el contacto y su versión 1 en una sola transacción. Devuelve el Id.</summary>
    Task<long> RegistrarAsync(ContactoARegistrar contacto, CancellationToken cancelacion);

    Task<ContactoDetalle?> ObtenerDetalleAsync(long contactoId, CancellationToken cancelacion);
}
