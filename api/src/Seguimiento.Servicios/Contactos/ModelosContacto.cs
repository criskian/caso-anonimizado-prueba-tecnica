using Seguimiento.Servicios.Catalogos;

namespace Seguimiento.Servicios.Contactos;

/// <summary>Datos que envía el gestor para registrar un contacto (CA-2).</summary>
public sealed record NuevoContacto(
    int PacienteId,
    DateTimeOffset FechaContacto,
    string Canal,
    string Resultado,
    string? Observacion);

/// <summary>Contacto ya validado, listo para guardarse como versión 1.</summary>
public sealed record ContactoARegistrar(
    int PacienteId,
    int GestorId,
    DateTimeOffset FechaContacto,
    string Canal,
    string Resultado,
    string? Observacion);

/// <summary>Lo que el servicio necesita saber del paciente para aceptar un contacto.</summary>
public sealed record PacienteParaContacto(int Id, string Estado, DateOnly FechaIngresoPrograma)
{
    public bool EstaActivo => Estado == "ACTIVO";
}

public sealed record PacienteDelContacto(int Id, string Nombre, string Ciudad);

public sealed record VersionContacto(
    int NumeroVersion,
    DateTimeOffset FechaContacto,
    ItemCatalogo Canal,
    ItemCatalogo Resultado,
    string? Observacion,
    string? MotivoCorreccion,
    GestorResumen RegistradoPor,
    DateTime RegistradoEnUtc);

/// <summary>Valores vigentes de un contacto y su historial completo de versiones.</summary>
public sealed record ContactoDetalle(
    long Id,
    PacienteDelContacto Paciente,
    GestorResumen Gestor,
    DateTimeOffset FechaContacto,
    ItemCatalogo Canal,
    ItemCatalogo Resultado,
    string? Observacion,
    int VersionActual,
    IReadOnlyList<VersionContacto> Historial);
