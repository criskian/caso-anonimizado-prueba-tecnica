using System.Globalization;
using Seguimiento.Servicios.Excepciones;

namespace Seguimiento.Servicios.Contactos;

/// <summary>
/// CA-4: vista de contactos de un mes, filtrable por gestor y por ciudad. Los filtros son
/// opcionales y, si vienen los dos, se combinan con Y. El gestor es quien hizo el contacto y
/// la ciudad es la actual del paciente (H-11, H-12).
/// </summary>
public sealed class ContactosDelMesService(IContactosDelMesRepositorio repositorio, TimeProvider reloj)
{
    public const int TamanoPorDefecto = 50;
    public const int TamanoMaximo = 200;

    public async Task<PaginaContactosDelMes> ConsultarAsync(
        string? mes, int? gestorId, int? ciudadId, int? pagina, int? tamano, CancellationToken cancelacion)
    {
        // Sin mes se usa el mes en curso en hora del programa, no el del servidor (H-13).
        var inicioMes = string.IsNullOrWhiteSpace(mes) ? InicioDelMesActual() : LeerMes(mes);

        var numeroPagina = pagina ?? 1;
        var tamanoPagina = tamano ?? TamanoPorDefecto;
        var errores = new Dictionary<string, string[]>();
        if (numeroPagina < 1)
        {
            errores["pagina"] = ["La página debe ser 1 o mayor."];
        }

        if (tamanoPagina is < 1 or > TamanoMaximo)
        {
            errores["tamano"] = [$"El tamaño de página debe estar entre 1 y {TamanoMaximo}."];
        }

        if (gestorId is <= 0)
        {
            errores["gestorId"] = ["El gestor debe ser un identificador positivo."];
        }

        if (ciudadId is <= 0)
        {
            errores["ciudadId"] = ["La ciudad debe ser un identificador positivo."];
        }

        if (errores.Count > 0)
        {
            var esDePaginacion = errores.ContainsKey("pagina") || errores.ContainsKey("tamano");
            throw new ValidacionException(errores, esDePaginacion ? "PAGINACION_INVALIDA" : "VALIDACION");
        }

        var desde = new DateTimeOffset(inicioMes.ToDateTime(TimeOnly.MinValue), HorarioPrograma.Desfase);
        var filtro = new FiltroContactosDelMes(desde, desde.AddMonths(1), gestorId, ciudadId, numeroPagina, tamanoPagina);

        var (items, total) = await repositorio.ConsultarAsync(filtro, cancelacion);
        return new PaginaContactosDelMes(inicioMes.ToString("yyyy-MM", CultureInfo.InvariantCulture), numeroPagina, tamanoPagina, total, items);
    }

    private DateOnly InicioDelMesActual()
    {
        var hoy = HorarioPrograma.FechaLocal(reloj.GetUtcNow());
        return new DateOnly(hoy.Year, hoy.Month, 1);
    }

    private static DateOnly LeerMes(string mes)
    {
        if (!DateOnly.TryParseExact(mes.Trim() + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var inicio))
        {
            throw new ValidacionException("mes", "El mes debe tener formato AAAA-MM, por ejemplo 2026-09.", "MES_INVALIDO");
        }

        return inicio;
    }
}
