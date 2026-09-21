using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Seguimiento.Servicios.Excepciones;

namespace Seguimiento.Api;

/// <summary>
/// Único punto donde las excepciones se traducen a HTTP. Toda respuesta de error sale como
/// ProblemDetails (RFC 7807) con una extensión «codigo» estable para la interfaz.
/// También es el único punto de registro: en appsettings.json se silencia el log del
/// middleware de .NET, que registraría como error incluso las excepciones de negocio esperadas.
/// </summary>
public sealed class ManejadorExcepciones(IProblemDetailsService problemDetails, ILogger<ManejadorExcepciones> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken cancelacion)
    {
        var (estado, titulo) = excepcion switch
        {
            ValidacionException => (StatusCodes.Status400BadRequest, "Datos inválidos"),
            GestorNoIdentificadoException => (StatusCodes.Status401Unauthorized, "Gestor no identificado"),
            NoEncontradoException => (StatusCodes.Status404NotFound, "No encontrado"),
            ConflictoException => (StatusCodes.Status409Conflict, "Conflicto"),
            ReglaDeNegocioException => (StatusCodes.Status422UnprocessableEntity, "Operación no permitida"),
            _ => (StatusCodes.Status500InternalServerError, "Error inesperado"),
        };

        var detalle = excepcion is ValidacionException validacion
            ? new ValidationProblemDetails(validacion.Errores.ToDictionary(e => e.Key, e => e.Value))
            : new ProblemDetails();
        detalle.Status = estado;
        detalle.Title = titulo;

        if (excepcion is ExcepcionDeDominio dominio)
        {
            detalle.Detail = dominio.Message;
            detalle.Extensions["codigo"] = dominio.Codigo;
        }
        else
        {
            // Un error no previsto se registra completo, pero al cliente no se le muestra el detalle interno.
            logger.LogError(excepcion, "Error no controlado en {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);
            detalle.Detail = "Ocurrió un error inesperado. Intenta de nuevo más tarde.";
            detalle.Extensions["codigo"] = "ERROR_INESPERADO";
        }

        contexto.Response.StatusCode = estado;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = detalle,
            Exception = excepcion,
        });
    }
}
