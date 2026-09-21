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

// El motivo no se marca como obligatorio aquí: su regla (10 a 500 caracteres) es de negocio
// y la aplica ContactoService, que es donde se prueba.
public sealed record CorregirContactoDto(
    [Required(ErrorMessage = "Indica sobre qué versión haces la corrección.")] int? VersionEsperada,
    [Required(ErrorMessage = "Indica la fecha y hora del contacto.")] DateTimeOffset? FechaContacto,
    [Required(ErrorMessage = "Indica el canal.")] string? Canal,
    [Required(ErrorMessage = "Indica el resultado.")] string? Resultado,
    string? Observacion,
    string? Motivo)
{
    public CorreccionContacto ACorreccion() =>
        new(VersionEsperada!.Value, FechaContacto!.Value, Canal!, Resultado!, Observacion, Motivo);
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

public sealed record ContactoMesItemDto(
    long Id,
    DateTimeOffset FechaContacto,
    ReferenciaDto Paciente,
    ReferenciaDto Ciudad,
    ReferenciaDto Gestor,
    CodigoNombreDto Canal,
    CodigoNombreDto Resultado,
    int VersionActual,
    bool Corregido);

public sealed record ContactosDelMesDto(string Mes, int Pagina, int Tamano, int Total, IReadOnlyList<ContactoMesItemDto> Items)
{
    public static ContactosDelMesDto Desde(PaginaContactosDelMes p) => new(
        p.Mes, p.Pagina, p.Tamano, p.Total,
        p.Items.Select(c => new ContactoMesItemDto(
            c.Id,
            c.FechaContacto,
            new ReferenciaDto(c.PacienteId, c.PacienteNombre),
            new ReferenciaDto(c.CiudadId, c.Ciudad),
            new ReferenciaDto(c.GestorId, c.GestorNombre),
            new CodigoNombreDto(c.CanalCodigo, c.CanalNombre),
            new CodigoNombreDto(c.ResultadoCodigo, c.ResultadoNombre),
            c.VersionActual,
            c.VersionActual > 1)).ToList());
}

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
