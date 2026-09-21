using Seguimiento.Servicios.Pacientes;

namespace Seguimiento.Api.Dtos;

public sealed record PacienteResumenDto(int Id, string Nombre, string Documento, string Ciudad)
{
    public static PacienteResumenDto Desde(PacienteResumen p) =>
        new(p.Id, p.Nombre, $"{p.TipoDocumento} {p.NumeroDocumento}", p.Ciudad);
}
