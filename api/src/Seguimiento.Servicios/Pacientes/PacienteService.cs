using Seguimiento.Servicios.Excepciones;

namespace Seguimiento.Servicios.Pacientes;

/// <summary>Búsqueda de pacientes para elegir a quién se le registra un contacto (CA-2).</summary>
public sealed class PacienteService(IPacienteRepositorio repositorio)
{
    public const int LargoMinimoBusqueda = 2;
    public const int MaximoResultados = 20;

    public Task<IReadOnlyList<PacienteResumen>> BuscarAsync(string? texto, CancellationToken cancelacion)
    {
        var busqueda = texto?.Trim() ?? string.Empty;
        if (busqueda.Length < LargoMinimoBusqueda)
        {
            throw new ValidacionException(
                "buscar", $"Escribe al menos {LargoMinimoBusqueda} caracteres del nombre o del documento.", "BUSQUEDA_CORTA");
        }

        return repositorio.BuscarActivosAsync(busqueda, MaximoResultados, cancelacion);
    }
}
