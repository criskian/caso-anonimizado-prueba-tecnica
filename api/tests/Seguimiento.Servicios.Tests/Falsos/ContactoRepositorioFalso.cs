using Seguimiento.Servicios.Catalogos;
using Seguimiento.Servicios.Contactos;

namespace Seguimiento.Servicios.Tests.Falsos;

/// <summary>
/// Repositorio en memoria para probar las reglas del servicio sin base de datos.
/// Imita el contrato de IContactoRepositorio (versiones que solo se agregan, corrección
/// condicionada a la versión esperada); lo que depende de SQL (transacciones, triggers,
/// consultas) se prueba en integración.
/// </summary>
public sealed class ContactoRepositorioFalso : IContactoRepositorio
{
    private sealed record Almacenado(long Id, int PacienteId, int GestorId, List<VersionContacto> Versiones);

    private readonly List<Almacenado> contactos = [];

    public Dictionary<int, PacienteParaContacto> Pacientes { get; } = [];
    public Dictionary<int, bool> Gestores { get; } = [];
    public Dictionary<string, bool> Canales { get; } = new() { ["LLAMADA"] = true, ["WHATSAPP"] = true, ["CORREO"] = true };
    public Dictionary<string, bool> Resultados { get; } = new() { ["EFECTIVO"] = true, ["NO_CONTESTA"] = true };
    public List<ContactoARegistrar> Registrados { get; } = [];
    public List<CorreccionARegistrar> Correcciones { get; } = [];

    /// <summary>Simula que otra corrección se guardó justo antes que la nuestra.</summary>
    public bool SimularCorreccionConcurrente { get; set; }

    public Task<PacienteParaContacto?> ObtenerPacienteAsync(int pacienteId, CancellationToken cancelacion) =>
        Task.FromResult(Pacientes.GetValueOrDefault(pacienteId));

    public Task<bool?> ObtenerEstadoGestorAsync(int gestorId, CancellationToken cancelacion) =>
        Task.FromResult(Gestores.TryGetValue(gestorId, out var activo) ? activo : (bool?)null);

    public Task<bool?> ObtenerEstadoCanalAsync(string codigo, CancellationToken cancelacion) =>
        Task.FromResult(Canales.TryGetValue(codigo, out var activo) ? activo : (bool?)null);

    public Task<bool?> ObtenerEstadoResultadoAsync(string codigo, CancellationToken cancelacion) =>
        Task.FromResult(Resultados.TryGetValue(codigo, out var activo) ? activo : (bool?)null);

    public Task<long> RegistrarAsync(ContactoARegistrar contacto, CancellationToken cancelacion)
    {
        Registrados.Add(contacto);
        var id = (long)contactos.Count + 1;
        contactos.Add(new Almacenado(id, contacto.PacienteId, contacto.GestorId,
        [
            Version(1, contacto.FechaContacto, contacto.Canal, contacto.Resultado, contacto.Observacion, null, contacto.GestorId),
        ]));
        return Task.FromResult(id);
    }

    public Task<bool> CorregirAsync(CorreccionARegistrar correccion, CancellationToken cancelacion)
    {
        var contacto = contactos.Single(c => c.Id == correccion.ContactoId);
        if (SimularCorreccionConcurrente)
        {
            contacto.Versiones.Add(contacto.Versiones[^1] with { NumeroVersion = contacto.Versiones.Count + 1 });
        }

        if (contacto.Versiones.Count != correccion.VersionEsperada)
        {
            return Task.FromResult(false);
        }

        Correcciones.Add(correccion);
        contacto.Versiones.Add(Version(
            correccion.VersionEsperada + 1, correccion.FechaContacto, correccion.Canal, correccion.Resultado,
            correccion.Observacion, correccion.Motivo, correccion.GestorId));
        return Task.FromResult(true);
    }

    public Task<ContactoDetalle?> ObtenerDetalleAsync(long contactoId, CancellationToken cancelacion)
    {
        var c = contactos.SingleOrDefault(x => x.Id == contactoId);
        if (c is null)
        {
            return Task.FromResult<ContactoDetalle?>(null);
        }

        var vigente = c.Versiones[^1];
        return Task.FromResult<ContactoDetalle?>(new ContactoDetalle(
            c.Id,
            new PacienteDelContacto(c.PacienteId, $"Paciente {c.PacienteId}", "Bogotá"),
            new GestorResumen(c.GestorId, $"Gestor {c.GestorId}"),
            vigente.FechaContacto, vigente.Canal, vigente.Resultado, vigente.Observacion,
            vigente.NumeroVersion, c.Versiones.ToList()));
    }

    private static VersionContacto Version(
        int numero, DateTimeOffset fecha, string canal, string resultado, string? observacion, string? motivo, int gestorId) =>
        new(numero, fecha, new ItemCatalogo(canal, canal), new ItemCatalogo(resultado, resultado),
            observacion, motivo, new GestorResumen(gestorId, $"Gestor {gestorId}"), DateTime.UtcNow);
}
