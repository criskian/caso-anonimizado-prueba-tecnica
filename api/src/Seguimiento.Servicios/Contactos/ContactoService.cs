using Seguimiento.Servicios.Excepciones;

namespace Seguimiento.Servicios.Contactos;

/// <summary>Reglas de negocio de los contactos (02-plan §4.4).</summary>
public sealed class ContactoService(IContactoRepositorio repositorio, TimeProvider reloj)
{
    public const int LargoMaximoObservacion = 500;

    // Margen para diferencias entre el reloj del dispositivo y el del servidor.
    public static readonly TimeSpan ToleranciaReloj = TimeSpan.FromMinutes(5);

    /// <summary>
    /// CA-2: registra un contacto asociado al paciente, con su fecha, canal y resultado.
    /// El orden de las comprobaciones define qué error ve el usuario si hay varios:
    /// identidad (401), datos (400), existencia (404) y reglas de negocio (422).
    /// </summary>
    public async Task<ContactoDetalle> RegistrarAsync(string? cabeceraGestor, NuevoContacto nuevo, CancellationToken cancelacion)
    {
        var gestorId = await IdentificarGestorAsync(cabeceraGestor, cancelacion);

        var observacion = string.IsNullOrWhiteSpace(nuevo.Observacion) ? null : nuevo.Observacion.Trim();
        var errores = new Dictionary<string, string[]>();
        if (observacion is { Length: > LargoMaximoObservacion })
        {
            errores["observacion"] = [$"La observación no puede superar {LargoMaximoObservacion} caracteres."];
        }

        var canalActivo = await repositorio.ObtenerEstadoCanalAsync(nuevo.Canal, cancelacion);
        if (canalActivo is null)
        {
            errores["canal"] = [$"El canal '{nuevo.Canal}' no existe."];
        }

        var resultadoActivo = await repositorio.ObtenerEstadoResultadoAsync(nuevo.Resultado, cancelacion);
        if (resultadoActivo is null)
        {
            errores["resultado"] = [$"El resultado '{nuevo.Resultado}' no existe."];
        }

        if (errores.Count > 0)
        {
            throw new ValidacionException(errores);
        }

        var paciente = await repositorio.ObtenerPacienteAsync(nuevo.PacienteId, cancelacion)
            ?? throw new NoEncontradoException("PACIENTE_NO_ENCONTRADO", $"No existe el paciente {nuevo.PacienteId}.");

        if (!paciente.EstaActivo)
        {
            throw new ReglaDeNegocioException("PACIENTE_INACTIVO", "El paciente está inactivo y no admite contactos nuevos.");
        }

        if (canalActivo == false || resultadoActivo == false)
        {
            throw new ReglaDeNegocioException("CATALOGO_INACTIVO", "El canal o el resultado elegido ya no está disponible.");
        }

        ValidarFechaContacto(nuevo.FechaContacto, paciente.FechaIngresoPrograma);

        var id = await repositorio.RegistrarAsync(
            new ContactoARegistrar(paciente.Id, gestorId, nuevo.FechaContacto, nuevo.Canal, nuevo.Resultado, observacion),
            cancelacion);

        return await repositorio.ObtenerDetalleAsync(id, cancelacion)
            ?? throw new InvalidOperationException($"El contacto {id} se registró pero no se pudo leer.");
    }

    public async Task<ContactoDetalle> ObtenerAsync(long contactoId, CancellationToken cancelacion) =>
        await repositorio.ObtenerDetalleAsync(contactoId, cancelacion)
            ?? throw new NoEncontradoException("CONTACTO_NO_ENCONTRADO", $"No existe el contacto {contactoId}.");

    // La identidad llega en la cabecera X-Gestor-Id: es una simulación declarada, no autenticación (H-11).
    private async Task<int> IdentificarGestorAsync(string? cabeceraGestor, CancellationToken cancelacion)
    {
        if (!int.TryParse(cabeceraGestor, out var gestorId) || gestorId <= 0)
        {
            throw new GestorNoIdentificadoException("Indica qué gestor hace la operación en la cabecera X-Gestor-Id.");
        }

        var activo = await repositorio.ObtenerEstadoGestorAsync(gestorId, cancelacion);
        return activo == true
            ? gestorId
            : throw new GestorNoIdentificadoException($"El gestor {gestorId} no existe o está inactivo.");
    }

    private void ValidarFechaContacto(DateTimeOffset fechaContacto, DateOnly fechaIngresoPrograma)
    {
        if (fechaContacto > reloj.GetUtcNow() + ToleranciaReloj)
        {
            throw new ReglaDeNegocioException("FECHA_FUTURA", "La fecha del contacto no puede estar en el futuro.");
        }

        // Se compara el día en hora local del programa, no en UTC (H-13).
        if (HorarioPrograma.FechaLocal(fechaContacto) < fechaIngresoPrograma)
        {
            throw new ReglaDeNegocioException(
                "FECHA_ANTERIOR_AL_INGRESO",
                $"La fecha del contacto es anterior al ingreso del paciente al programa ({fechaIngresoPrograma:yyyy-MM-dd}).");
        }
    }
}
