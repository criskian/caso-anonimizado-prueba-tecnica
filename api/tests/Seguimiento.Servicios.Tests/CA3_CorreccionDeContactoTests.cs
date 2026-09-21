using Microsoft.Extensions.Time.Testing;
using Seguimiento.Servicios.Contactos;
using Seguimiento.Servicios.Excepciones;
using Seguimiento.Servicios.Tests.Falsos;

namespace Seguimiento.Servicios.Tests;

/// <summary>
/// CA-3: dado un contacto registrado con error, cuando el gestor lo corrige, entonces el
/// reporte refleja la información corregida, sin perder la versión original (H-01).
/// Que la vista del mes muestre la versión corregida se prueba en integración.
/// </summary>
public sealed class CA3_CorreccionDeContactoTests
{
    private const int Autor = 1;
    private const int OtroGestor = 3;
    private const int Paciente = 10;
    private const string Motivo = "El paciente sí contestó; se registró mal el resultado.";

    private static readonly DateTimeOffset Ahora = new(2026, 9, 21, 15, 0, 0, TimeSpan.FromHours(-5));
    private static readonly DateTimeOffset FechaOriginal = Ahora.AddDays(-2);

    private readonly ContactoRepositorioFalso repositorio = new();
    private readonly ContactoService servicio;
    private readonly long contactoId;

    public CA3_CorreccionDeContactoTests()
    {
        repositorio.Gestores[Autor] = true;
        repositorio.Gestores[OtroGestor] = true;
        repositorio.Pacientes[Paciente] = new PacienteParaContacto(Paciente, "ACTIVO", new DateOnly(2026, 9, 1));
        servicio = new ContactoService(repositorio, new FakeTimeProvider(Ahora));

        contactoId = servicio.RegistrarAsync(
            Autor.ToString(),
            new NuevoContacto(Paciente, FechaOriginal, "LLAMADA", "NO_CONTESTA", "Sin respuesta"),
            default).GetAwaiter().GetResult().Id;
    }

    private static CorreccionContacto Correccion(
        int versionEsperada = 1,
        DateTimeOffset? fecha = null,
        string canal = "LLAMADA",
        string resultado = "EFECTIVO",
        string? observacion = "Sin respuesta",
        string? motivo = Motivo) =>
        new(versionEsperada, fecha ?? FechaOriginal, canal, resultado, observacion, motivo);

    [Fact]
    public async Task CA3_Corregir_CreaVersionNuevaYConservaLaAnterior()
    {
        var detalle = await servicio.CorregirAsync(OtroGestor.ToString(), contactoId, Correccion(), default);

        Assert.Equal(2, detalle.VersionActual);
        Assert.Equal("EFECTIVO", detalle.Resultado.Codigo);
        Assert.Equal(2, detalle.Historial.Count);

        var original = detalle.Historial[0];
        Assert.Equal(1, original.NumeroVersion);
        Assert.Equal("NO_CONTESTA", original.Resultado.Codigo);
        Assert.Null(original.MotivoCorreccion);

        var corregida = detalle.Historial[1];
        Assert.Equal(2, corregida.NumeroVersion);
        Assert.Equal("EFECTIVO", corregida.Resultado.Codigo);
        Assert.Equal(Motivo, corregida.MotivoCorreccion);
    }

    [Fact]
    public async Task CA3_Corregir_RegistraQuienCorrigeSinCambiarElAutorOriginal()
    {
        var detalle = await servicio.CorregirAsync(OtroGestor.ToString(), contactoId, Correccion(), default);

        Assert.Equal(Autor, detalle.Gestor.Id);
        Assert.Equal(Autor, detalle.Historial[0].RegistradoPor.Id);
        Assert.Equal(OtroGestor, detalle.Historial[1].RegistradoPor.Id);
        Assert.Equal(OtroGestor, Assert.Single(repositorio.Correcciones).GestorId);
    }

    [Fact]
    public async Task CA3_Corregir_DosVecesSeguidas_EncadenaLasVersiones()
    {
        await servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(), default);
        var detalle = await servicio.CorregirAsync(
            Autor.ToString(), contactoId, Correccion(versionEsperada: 2, canal: "WHATSAPP"), default);

        Assert.Equal(3, detalle.VersionActual);
        Assert.Equal([1, 2, 3], detalle.Historial.Select(v => v.NumeroVersion));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   corto   ")]
    public async Task CA3_Corregir_SinMotivoSuficiente_LanzaValidacion(string? motivo)
    {
        var error = await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(motivo: motivo), default));

        Assert.Contains("motivo", error.Errores.Keys);
        Assert.Empty(repositorio.Correcciones);
    }

    [Fact]
    public async Task CA3_Corregir_MotivoConEspacios_SeGuardaRecortado()
    {
        await servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(motivo: $"   {Motivo}   "), default);

        Assert.Equal(Motivo, Assert.Single(repositorio.Correcciones).Motivo);
    }

    [Fact]
    public async Task CA3_Corregir_VersionDesactualizada_LanzaConflicto()
    {
        await servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(), default);

        // Un segundo usuario corrige sobre la versión 1, que ya no es la vigente.
        var error = await Assert.ThrowsAsync<ConflictoException>(
            () => servicio.CorregirAsync(OtroGestor.ToString(), contactoId, Correccion(canal: "CORREO"), default));

        Assert.Equal("VERSION_DESACTUALIZADA", error.Codigo);
        Assert.Single(repositorio.Correcciones);
    }

    [Fact]
    public async Task CA3_Corregir_OtraCorreccionLlegaEntreLecturaYEscritura_LanzaConflicto()
    {
        repositorio.SimularCorreccionConcurrente = true;

        var error = await Assert.ThrowsAsync<ConflictoException>(
            () => servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(), default));

        Assert.Equal("VERSION_DESACTUALIZADA", error.Codigo);
        Assert.Empty(repositorio.Correcciones);
    }

    [Fact]
    public async Task CA3_Corregir_SinCambios_RechazaPorReglaDeNegocio()
    {
        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(resultado: "NO_CONTESTA"), default));

        Assert.Equal("SIN_CAMBIOS", error.Codigo);
    }

    [Fact]
    public async Task CA3_Corregir_ContactoInexistente_LanzaNoEncontrado()
    {
        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => servicio.CorregirAsync(Autor.ToString(), 999, Correccion(), default));

        Assert.Equal("CONTACTO_NO_ENCONTRADO", error.Codigo);
    }

    [Fact]
    public async Task CA3_Corregir_FechaFutura_RechazaPorReglaDeNegocio()
    {
        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(fecha: Ahora.AddHours(1)), default));

        Assert.Equal("FECHA_FUTURA", error.Codigo);
    }

    [Fact]
    public async Task CA3_Corregir_PacienteQueHoyEstaInactivo_SePermite()
    {
        repositorio.Pacientes[Paciente] = repositorio.Pacientes[Paciente] with { Estado = "INACTIVO" };

        var detalle = await servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(), default);

        Assert.Equal(2, detalle.VersionActual);
    }

    [Fact]
    public async Task CA3_Corregir_ConservarCanalRetirado_SePermitePeroElegirloComoNuevoNo()
    {
        repositorio.Canales["LLAMADA"] = false;
        repositorio.Canales["CORREO"] = false;

        // El contacto ya era por LLAMADA: puede corregirse el resultado sin cambiar el canal.
        await servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(), default);

        // Pero no se puede cambiar a otro canal retirado.
        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.CorregirAsync(Autor.ToString(), contactoId, Correccion(versionEsperada: 2, canal: "CORREO"), default));
        Assert.Equal("CATALOGO_INACTIVO", error.Codigo);
    }

    [Fact]
    public async Task CA3_Corregir_SinGestorValido_LanzaGestorNoIdentificado()
    {
        await Assert.ThrowsAsync<GestorNoIdentificadoException>(
            () => servicio.CorregirAsync(null, contactoId, Correccion(), default));

        Assert.Empty(repositorio.Correcciones);
    }
}
