using Seguimiento.Servicios.Catalogos;
using Seguimiento.Servicios.Contactos;

namespace Seguimiento.Servicios.Tests.Falsos;

/// <summary>
/// Repositorio en memoria para probar las reglas del servicio sin base de datos.
/// Lo que depende de SQL (transacciones, triggers, consultas) se prueba en integración.
/// </summary>
public sealed class ContactoRepositorioFalso : IContactoRepositorio
{
    public Dictionary<int, PacienteParaContacto> Pacientes { get; } = [];
    public Dictionary<int, bool> Gestores { get; } = [];
    public Dictionary<string, bool> Canales { get; } = new() { ["LLAMADA"] = true, ["WHATSAPP"] = true, ["CORREO"] = true };
    public Dictionary<string, bool> Resultados { get; } = new() { ["EFECTIVO"] = true, ["NO_CONTESTA"] = true };
    public List<ContactoARegistrar> Registrados { get; } = [];

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
        return Task.FromResult((long)Registrados.Count);
    }

    public Task<ContactoDetalle?> ObtenerDetalleAsync(long contactoId, CancellationToken cancelacion)
    {
        if (contactoId < 1 || contactoId > Registrados.Count)
        {
            return Task.FromResult<ContactoDetalle?>(null);
        }

        var c = Registrados[(int)contactoId - 1];
        var gestor = new GestorResumen(c.GestorId, $"Gestor {c.GestorId}");
        var canal = new ItemCatalogo(c.Canal, c.Canal);
        var resultado = new ItemCatalogo(c.Resultado, c.Resultado);
        var version = new VersionContacto(1, c.FechaContacto, canal, resultado, c.Observacion, null, gestor, DateTime.UtcNow);

        return Task.FromResult<ContactoDetalle?>(new ContactoDetalle(
            contactoId, new PacienteDelContacto(c.PacienteId, $"Paciente {c.PacienteId}", "Bogotá"), gestor,
            c.FechaContacto, canal, resultado, c.Observacion, 1, [version]));
    }
}
