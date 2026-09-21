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

    /// <summary>
    /// Guarda la corrección como versión nueva y actualiza la proyección en una sola transacción,
    /// solo si la versión vigente sigue siendo VersionEsperada. Devuelve false si otra
    /// corrección llegó antes (concurrencia optimista); en ese caso no escribe nada.
    /// </summary>
    Task<bool> CorregirAsync(CorreccionARegistrar correccion, CancellationToken cancelacion);

    Task<ContactoDetalle?> ObtenerDetalleAsync(long contactoId, CancellationToken cancelacion);
}
