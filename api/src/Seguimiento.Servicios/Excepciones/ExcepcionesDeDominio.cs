namespace Seguimiento.Servicios.Excepciones;

// Excepciones que lanzan los servicios cuando una operación no se puede completar.
// Cada una lleva un código estable que la interfaz usa para decidir qué mostrar.
// La API las traduce a HTTP en un único lugar (ManejadorExcepciones); los servicios
// no conocen los códigos de estado.

public abstract class ExcepcionDeDominio(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

/// <summary>Los datos recibidos no son válidos (HTTP 400). Incluye los errores por campo.</summary>
public sealed class ValidacionException(IReadOnlyDictionary<string, string[]> errores, string codigo = "VALIDACION")
    : ExcepcionDeDominio(codigo, "Uno o más datos no son válidos.")
{
    public IReadOnlyDictionary<string, string[]> Errores { get; } = errores;

    public ValidacionException(string campo, string error, string codigo = "VALIDACION")
        : this(new Dictionary<string, string[]> { [campo] = [error] }, codigo)
    {
    }
}

/// <summary>El recurso pedido no existe (HTTP 404).</summary>
public sealed class NoEncontradoException(string codigo, string mensaje)
    : ExcepcionDeDominio(codigo, mensaje);

/// <summary>Los datos son válidos pero una regla de negocio impide la operación (HTTP 422).</summary>
public sealed class ReglaDeNegocioException(string codigo, string mensaje)
    : ExcepcionDeDominio(codigo, mensaje);

/// <summary>La operación choca con el estado actual del recurso, por ejemplo una versión desactualizada (HTTP 409).</summary>
public sealed class ConflictoException(string codigo, string mensaje)
    : ExcepcionDeDominio(codigo, mensaje);

/// <summary>No se pudo identificar al gestor que hace la operación (HTTP 401).</summary>
public sealed class GestorNoIdentificadoException(string mensaje)
    : ExcepcionDeDominio("GESTOR_NO_IDENTIFICADO", mensaje);
