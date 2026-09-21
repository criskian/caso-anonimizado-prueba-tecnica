using Microsoft.Extensions.Time.Testing;
using Seguimiento.Servicios.Contactos;
using Seguimiento.Servicios.Excepciones;
using Seguimiento.Servicios.Tests.Falsos;

namespace Seguimiento.Servicios.Tests;

/// <summary>
/// CA-2: dado un paciente registrado, cuando el gestor registra un contacto, entonces el
/// contacto queda asociado al paciente con su fecha, canal y resultado.
/// </summary>
public sealed class CA2_RegistroDeContactoTests
{
    private const int GestorActivo = 1;
    private const int GestorInactivo = 2;
    private const int PacienteActivo = 10;
    private const int PacienteInactivo = 11;

    // «Ahora» fijo: 21 de septiembre de 2026 a las 15:00 en hora del programa (UTC−5).
    private static readonly DateTimeOffset Ahora = new(2026, 9, 21, 15, 0, 0, TimeSpan.FromHours(-5));
    private static readonly DateOnly IngresoAlPrograma = new(2026, 9, 1);

    private readonly ContactoRepositorioFalso repositorio = new();
    private readonly ContactoService servicio;

    public CA2_RegistroDeContactoTests()
    {
        repositorio.Gestores[GestorActivo] = true;
        repositorio.Gestores[GestorInactivo] = false;
        repositorio.Pacientes[PacienteActivo] = new PacienteParaContacto(PacienteActivo, "ACTIVO", IngresoAlPrograma);
        repositorio.Pacientes[PacienteInactivo] = new PacienteParaContacto(PacienteInactivo, "INACTIVO", IngresoAlPrograma);
        servicio = new ContactoService(repositorio, new FakeTimeProvider(Ahora));
    }

    private static NuevoContacto Contacto(
        int pacienteId = PacienteActivo,
        DateTimeOffset? fecha = null,
        string canal = "LLAMADA",
        string resultado = "EFECTIVO",
        string? observacion = null) =>
        new(pacienteId, fecha ?? Ahora.AddHours(-2), canal, resultado, observacion);

    [Fact]
    public async Task CA2_RegistrarContacto_PacienteActivo_QuedaAsociadoConFechaCanalYResultado()
    {
        var fecha = Ahora.AddHours(-3);

        var detalle = await servicio.RegistrarAsync("1", Contacto(fecha: fecha, canal: "WHATSAPP", resultado: "NO_CONTESTA"), default);

        var guardado = Assert.Single(repositorio.Registrados);
        Assert.Equal(PacienteActivo, guardado.PacienteId);
        Assert.Equal(GestorActivo, guardado.GestorId);
        Assert.Equal(fecha, guardado.FechaContacto);
        Assert.Equal("WHATSAPP", guardado.Canal);
        Assert.Equal("NO_CONTESTA", guardado.Resultado);
        Assert.Equal(PacienteActivo, detalle.Paciente.Id);
        Assert.Equal(1, detalle.VersionActual);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_ObservacionConEspacios_SeGuardaRecortadaYVaciaComoNula()
    {
        await servicio.RegistrarAsync("1", Contacto(observacion: "  Confirma dispensación  "), default);
        await servicio.RegistrarAsync("1", Contacto(observacion: "   "), default);

        Assert.Equal("Confirma dispensación", repositorio.Registrados[0].Observacion);
        Assert.Null(repositorio.Registrados[1].Observacion);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_FechaFutura_RechazaPorReglaDeNegocio()
    {
        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.RegistrarAsync("1", Contacto(fecha: Ahora.AddMinutes(10)), default));

        Assert.Equal("FECHA_FUTURA", error.Codigo);
        Assert.Empty(repositorio.Registrados);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_DentroDeLaToleranciaDeReloj_SeAcepta()
    {
        await servicio.RegistrarAsync("1", Contacto(fecha: Ahora.AddMinutes(4)), default);

        Assert.Single(repositorio.Registrados);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_FechaAnteriorAlIngreso_RechazaPorReglaDeNegocio()
    {
        // 02:00 UTC del 1 de septiembre son las 21:00 del 31 de agosto en hora del programa:
        // en UTC parecería el día del ingreso, pero en hora local es el día anterior (H-13).
        var nocheAnteriorAlIngreso = new DateTimeOffset(2026, 9, 1, 2, 0, 0, TimeSpan.Zero);

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.RegistrarAsync("1", Contacto(fecha: nocheAnteriorAlIngreso), default));

        Assert.Equal("FECHA_ANTERIOR_AL_INGRESO", error.Codigo);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_ElMismoDiaDelIngreso_SeAcepta()
    {
        var primeraHoraDelIngreso = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.FromHours(-5));

        await servicio.RegistrarAsync("1", Contacto(fecha: primeraHoraDelIngreso), default);

        Assert.Single(repositorio.Registrados);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_PacienteInexistente_LanzaNoEncontrado()
    {
        var error = await Assert.ThrowsAsync<NoEncontradoException>(
            () => servicio.RegistrarAsync("1", Contacto(pacienteId: 999), default));

        Assert.Equal("PACIENTE_NO_ENCONTRADO", error.Codigo);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_PacienteInactivo_RechazaPorReglaDeNegocio()
    {
        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.RegistrarAsync("1", Contacto(pacienteId: PacienteInactivo), default));

        Assert.Equal("PACIENTE_INACTIVO", error.Codigo);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_CanalYResultadoInexistentes_LanzaValidacionConAmbosCampos()
    {
        var error = await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.RegistrarAsync("1", Contacto(canal: "SMS", resultado: "OTRO"), default));

        Assert.Contains("canal", error.Errores.Keys);
        Assert.Contains("resultado", error.Errores.Keys);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_CanalInactivo_RechazaPorReglaDeNegocio()
    {
        repositorio.Canales["CORREO"] = false;

        var error = await Assert.ThrowsAsync<ReglaDeNegocioException>(
            () => servicio.RegistrarAsync("1", Contacto(canal: "CORREO"), default));

        Assert.Equal("CATALOGO_INACTIVO", error.Codigo);
    }

    [Fact]
    public async Task CA2_RegistrarContacto_ObservacionDemasiadoLarga_LanzaValidacion()
    {
        var larga = new string('x', ContactoService.LargoMaximoObservacion + 1);

        var error = await Assert.ThrowsAsync<ValidacionException>(
            () => servicio.RegistrarAsync("1", Contacto(observacion: larga), default));

        Assert.Contains("observacion", error.Errores.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("999")]
    [InlineData("2")] // gestor inactivo
    public async Task CA2_RegistrarContacto_SinGestorValido_LanzaGestorNoIdentificado(string? cabecera)
    {
        await Assert.ThrowsAsync<GestorNoIdentificadoException>(
            () => servicio.RegistrarAsync(cabecera, Contacto(), default));

        Assert.Empty(repositorio.Registrados);
    }
}
