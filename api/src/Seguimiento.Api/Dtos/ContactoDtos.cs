using System.ComponentModel.DataAnnotations;
using Seguimiento.Servicios.Contactos;

namespace Seguimiento.Api.Dtos;

// Las anotaciones solo validan la forma (que el dato venga). Las reglas de negocio
// (fechas, catálogos, estado del paciente, largo de la observación) viven en ContactoService.

public sealed record RegistrarContactoDto(
    [Required(ErrorMessage = "Indica el paciente.")] int? PacienteId,
    [Required(ErrorMessage = "Indica la fecha y hora del contacto.")] DateTimeOffset? FechaContacto,
    [Required(ErrorMessage = "Indica el canal.")] string? Canal,
    [Required(ErrorMessage = "Indica el resultado.")] string? Resultado,
    string? Observacion)
{
    public NuevoContacto ANuevoContacto() =>
        new(PacienteId!.Value, FechaContacto!.Value, Canal!, Resultado!, Observacion);
}

public sealed record ReferenciaDto(int Id, string Nombre);

public sealed record PacienteDelContactoDto(int Id, string Nombre, string Ciudad);

public sealed record CodigoNombreDto(string Codigo, string Nombre);

public sealed record VersionContactoDto(
    int NumeroVersion,
    DateTimeOffset FechaContacto,
    CodigoNombreDto Canal,
    CodigoNombreDto Resultado,
    string? Observacion,
    string? MotivoCorreccion,
    ReferenciaDto RegistradoPor,
    DateTime RegistradoEnUtc);

public sealed record ContactoDetalleDto(
    long Id,
    PacienteDelContactoDto Paciente,
    ReferenciaDto Gestor,
    DateTimeOffset FechaContacto,
    CodigoNombreDto Canal,
    CodigoNombreDto Resultado,
    string? Observacion,
    int VersionActual,
    IReadOnlyList<VersionContactoDto> Historial)
{
    public static ContactoDetalleDto Desde(ContactoDetalle c) => new(
        c.Id,
        new PacienteDelContactoDto(c.Paciente.Id, c.Paciente.Nombre, c.Paciente.Ciudad),
        new ReferenciaDto(c.Gestor.Id, c.Gestor.Nombre),
        c.FechaContacto,
        new CodigoNombreDto(c.Canal.Codigo, c.Canal.Nombre),
        new CodigoNombreDto(c.Resultado.Codigo, c.Resultado.Nombre),
        c.Observacion,
        c.VersionActual,
        c.Historial.Select(v => new VersionContactoDto(
            v.NumeroVersion,
            v.FechaContacto,
            new CodigoNombreDto(v.Canal.Codigo, v.Canal.Nombre),
            new CodigoNombreDto(v.Resultado.Codigo, v.Resultado.Nombre),
            v.Observacion,
            v.MotivoCorreccion,
            new ReferenciaDto(v.RegistradoPor.Id, v.RegistradoPor.Nombre),
            v.RegistradoEnUtc)).ToList());
}
