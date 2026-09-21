namespace Seguimiento.Servicios;

/// <summary>
/// Hora local del programa (H-13): Colombia, Perú y Ecuador continental están en UTC−5 y
/// ninguno cambia de hora. A qué día o mes pertenece un contacto se decide con esta hora.
/// </summary>
public static class HorarioPrograma
{
    public static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    public static DateOnly FechaLocal(DateTimeOffset momento) =>
        DateOnly.FromDateTime(momento.ToOffset(Desfase).DateTime);
}
