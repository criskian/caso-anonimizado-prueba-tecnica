using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Seguimiento.Api.Json;

/// <summary>
/// Exige que toda fecha-hora recibida traiga su desfase («Z» o «±hh:mm»). Sin él,
/// System.Text.Json asumiría la zona del servidor y un contacto podría caer en otro día
/// o en otro mes (H-13). Una fecha sin desfase se rechaza con 400.
/// </summary>
public sealed partial class FechaConDesfaseConverter : JsonConverter<DateTimeOffset>
{
    [GeneratedRegex(@"(Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex TerminaConDesfase();

    public override DateTimeOffset Read(ref Utf8JsonReader lector, Type tipo, JsonSerializerOptions opciones)
    {
        var texto = lector.GetString();
        if (texto is null
            || !TerminaConDesfase().IsMatch(texto)
            || !DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out var valor))
        {
            throw new JsonException("La fecha debe tener formato ISO 8601 con desfase horario, por ejemplo 2026-09-21T10:30:00-05:00.");
        }

        return valor;
    }

    public override void Write(Utf8JsonWriter escritor, DateTimeOffset valor, JsonSerializerOptions opciones) =>
        escritor.WriteStringValue(valor);
}
