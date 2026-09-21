using Seguimiento.Servicios.Excepciones;

namespace Seguimiento.Servicios.Contactos;

/// <summary>
/// Reglas de negocio de los contactos (02-plan §4.4).
/// El orden de las comprobaciones define qué error ve el usuario si hay varios:
/// identidad (401), datos (400), existencia (404), versión (409) y reglas de negocio (422).
/// </summary>
public sealed class ContactoService(IContactoRepositorio repositorio, TimeProvider reloj)
{
    public const int LargoMaximoObservacion = 500;
    public const int LargoMinimoMotivo = 10;
    public const int LargoMaximoMotivo = 500;

    // Margen para diferencias entre el reloj del dispositivo y el del servidor.
    public static readonly TimeSpan ToleranciaReloj = TimeSpan.FromMinutes(5);

    /// <summary>CA-2: registra un contacto asociado al paciente, con su fecha, canal y resultado.</summary>
    public async Task<ContactoDetalle> RegistrarAsync(string? cabeceraGestor, NuevoContacto nuevo, CancellationToken cancelacion)
    {
        var gestorId = await IdentificarGestorAsync(cabeceraGestor, cancelacion);

        var errores = new Dictionary<string, string[]>();
        var observacion = NormalizarObservacion(nuevo.Observacion, errores);
        var (canalActivo, resultadoActivo) = await ValidarCatalogosAsync(nuevo.Canal, nuevo.Resultado, errores, cancelacion);
        LanzarSiHayErrores(errores);

        var paciente = await repositorio.ObtenerPacienteAsync(nuevo.PacienteId, cancelacion)
            ?? throw new NoEncontradoException("PACIENTE_NO_ENCONTRADO", $"No existe el paciente {nuevo.PacienteId}.");

        if (!paciente.EstaActivo)
        {
            throw new ReglaDeNegocioException("PACIENTE_INACTIVO", "El paciente está inactivo y no admite contactos nuevos.");
        }

        if (!canalActivo || !resultadoActivo)
        {
            throw CatalogoInactivo();
        }

        ValidarFechaContacto(nuevo.FechaContacto, paciente.FechaIngresoPrograma);

        var id = await repositorio.RegistrarAsync(
            new ContactoARegistrar(paciente.Id, gestorId, nuevo.FechaContacto, nuevo.Canal, nuevo.Resultado, observacion),
            cancelacion);

        return await LeerDespuesDeEscribirAsync(id, cancelacion);
    }

    /// <summary>
    /// CA-3: corrige un contacto creando una versión nueva. La versión anterior no se toca (H-01)
    /// y queda registrado quién corrigió, cuándo y por qué. El paciente y el autor original no cambian.
    /// </summary>
    public async Task<ContactoDetalle> CorregirAsync(
        string? cabeceraGestor, long contactoId, CorreccionContacto correccion, CancellationToken cancelacion)
    {
        var gestorId = await IdentificarGestorAsync(cabeceraGestor, cancelacion);

        var errores = new Dictionary<string, string[]>();
        var motivo = correccion.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length < LargoMinimoMotivo || motivo.Length > LargoMaximoMotivo)
        {
            errores["motivo"] = [$"Explica el motivo de la corrección en {LargoMinimoMotivo} a {LargoMaximoMotivo} caracteres."];
        }

        var observacion = NormalizarObservacion(correccion.Observacion, errores);
        var (canalActivo, resultadoActivo) = await ValidarCatalogosAsync(correccion.Canal, correccion.Resultado, errores, cancelacion);
        LanzarSiHayErrores(errores);

        var actual = await ObtenerAsync(contactoId, cancelacion);

        if (correccion.VersionEsperada != actual.VersionActual)
        {
            throw VersionDesactualizada(actual.VersionActual);
        }

        // Un canal o resultado retirado del catálogo puede conservarse si ya estaba en el
        // contacto; lo que no se permite es elegirlo como valor nuevo.
        if ((!canalActivo && correccion.Canal != actual.Canal.Codigo)
            || (!resultadoActivo && correccion.Resultado != actual.Resultado.Codigo))
        {
            throw CatalogoInactivo();
        }

        // Se permite corregir contactos de pacientes que hoy están inactivos: se corrige historia.
        var paciente = await repositorio.ObtenerPacienteAsync(actual.Paciente.Id, cancelacion)
            ?? throw new InvalidOperationException($"El contacto {contactoId} apunta a un paciente que no existe.");
        ValidarFechaContacto(correccion.FechaContacto, paciente.FechaIngresoPrograma);

        if (correccion.FechaContacto == actual.FechaContacto
            && correccion.Canal == actual.Canal.Codigo
            && correccion.Resultado == actual.Resultado.Codigo
            && observacion == actual.Observacion)
        {
            throw new ReglaDeNegocioException("SIN_CAMBIOS", "La corrección no cambia ningún dato del contacto.");
        }

        var guardada = await repositorio.CorregirAsync(
            new CorreccionARegistrar(
                contactoId, correccion.VersionEsperada, gestorId, correccion.FechaContacto,
                correccion.Canal, correccion.Resultado, observacion, motivo),
            cancelacion);

        if (!guardada)
        {
            // Otra corrección se guardó entre la lectura y la escritura.
            var vigente = await ObtenerAsync(contactoId, cancelacion);
            throw VersionDesactualizada(vigente.VersionActual);
        }

        return await LeerDespuesDeEscribirAsync(contactoId, cancelacion);
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

    private static string? NormalizarObservacion(string? observacion, Dictionary<string, string[]> errores)
    {
        var normalizada = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        if (normalizada is { Length: > LargoMaximoObservacion })
        {
            errores["observacion"] = [$"La observación no puede superar {LargoMaximoObservacion} caracteres."];
        }

        return normalizada;
    }

    // Registra en «errores» los códigos que no existen (400) y devuelve si cada uno está activo.
    private async Task<(bool CanalActivo, bool ResultadoActivo)> ValidarCatalogosAsync(
        string canal, string resultado, Dictionary<string, string[]> errores, CancellationToken cancelacion)
    {
        var canalActivo = await repositorio.ObtenerEstadoCanalAsync(canal, cancelacion);
        if (canalActivo is null)
        {
            errores["canal"] = [$"El canal '{canal}' no existe."];
        }

        var resultadoActivo = await repositorio.ObtenerEstadoResultadoAsync(resultado, cancelacion);
        if (resultadoActivo is null)
        {
            errores["resultado"] = [$"El resultado '{resultado}' no existe."];
        }

        return (canalActivo == true, resultadoActivo == true);
    }

    private static void LanzarSiHayErrores(Dictionary<string, string[]> errores)
    {
        if (errores.Count > 0)
        {
            throw new ValidacionException(errores);
        }
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

    private async Task<ContactoDetalle> LeerDespuesDeEscribirAsync(long contactoId, CancellationToken cancelacion) =>
        await repositorio.ObtenerDetalleAsync(contactoId, cancelacion)
            ?? throw new InvalidOperationException($"El contacto {contactoId} se guardó pero no se pudo leer.");

    private static ReglaDeNegocioException CatalogoInactivo() =>
        new("CATALOGO_INACTIVO", "El canal o el resultado elegido ya no está disponible.");

    private static ConflictoException VersionDesactualizada(int versionVigente) =>
        new("VERSION_DESACTUALIZADA",
            $"Otro usuario corrigió este contacto (versión vigente: {versionVigente}). Recarga para ver los datos actuales.");
}
