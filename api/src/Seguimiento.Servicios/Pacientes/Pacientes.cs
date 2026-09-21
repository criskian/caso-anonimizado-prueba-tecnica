namespace Seguimiento.Servicios.Pacientes;

public sealed record PacienteResumen(int Id, string Nombre, string TipoDocumento, string NumeroDocumento, string Ciudad);

public interface IPacienteRepositorio
{
    /// <summary>Pacientes activos cuyo nombre o número de documento contiene el texto.</summary>
    Task<IReadOnlyList<PacienteResumen>> BuscarActivosAsync(string texto, int maximo, CancellationToken cancelacion);
}
