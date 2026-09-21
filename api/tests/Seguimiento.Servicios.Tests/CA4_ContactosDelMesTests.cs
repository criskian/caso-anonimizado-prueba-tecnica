using Microsoft.Extensions.Time.Testing;
using Seguimiento.Servicios.Contactos;
using Seguimiento.Servicios.Excepciones;

namespace Seguimiento.Servicios.Tests;

/// <summary>
/// CA-4, reglas del servicio: cómo se interpreta el mes y la paginación. El filtro por gestor
/// y ciudad vive en SQL y se prueba en integración contra SQL Server real.
/// </summary>
public sealed class CA4_ContactosDelMesTests
{
    private sealed class RepositorioFalso : IContactosDelMesRepositorio
    {
        public FiltroContactosDelMes? FiltroRecibido { get; private set; }

        public Task<(IReadOnlyList<ContactoDelMes> Items, int Total)> ConsultarAsync(
            FiltroContactosDelMes filtro, CancellationToken cancelacion)
        {
            FiltroRecibido = filtro;
            return Task.FromResult<(IReadOnlyList<ContactoDelMes>, int)>(([], 0));
        }
    }

    private static readonly TimeSpan UtcMenos5 = TimeSpan.FromHours(-5);
    private readonly RepositorioFalso repositorio = new();

    private ContactosDelMesService Servicio(DateTimeOffset ahora) => new(repositorio, new FakeTimeProvider(ahora));

    [Fact]
    public async Task CA4_MesIndicado_ConsultaElRangoDelMesEnHoraLocal()
    {
        var pagina = await Servicio(DateTimeOffset.UtcNow).ConsultarAsync("2026-09", 3, 7, null, null, default);

        var filtro = repositorio.FiltroRecibido!;
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 0, 0, 0, UtcMenos5), filtro.Desde);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 0, 0, 0, UtcMenos5), filtro.Hasta);
        Assert.Equal(3, filtro.GestorId);
        Assert.Equal(7, filtro.CiudadId);
        Assert.Equal("2026-09", pagina.Mes);
    }

    [Fact]
    public async Task CA4_SinMes_UsaElMesEnCursoEnHoraLocalNoEnUtc()
    {
        // 1 de octubre a las 02:00 UTC: en hora del programa todavía es 30 de septiembre.
        var ahora = new DateTimeOffset(2026, 10, 1, 2, 0, 0, TimeSpan.Zero);

        var pagina = await Servicio(ahora).ConsultarAsync(null, null, null, null, null, default);

        Assert.Equal("2026-09", pagina.Mes);
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 0, 0, 0, UtcMenos5), repositorio.FiltroRecibido!.Desde);
    }

    [Fact]
    public async Task CA4_SinFiltrosNiPaginacion_UsaValoresPorDefecto()
    {
        await Servicio(DateTimeOffset.UtcNow).ConsultarAsync("2026-12", null, null, null, null, default);

        var filtro = repositorio.FiltroRecibido!;
        Assert.Null(filtro.GestorId);
        Assert.Null(filtro.CiudadId);
        Assert.Equal(1, filtro.Pagina);
        Assert.Equal(ContactosDelMesService.TamanoPorDefecto, filtro.Tamano);
        // Diciembre termina en enero del año siguiente.
        Assert.Equal(new DateTimeOffset(2027, 1, 1, 0, 0, 0, UtcMenos5), filtro.Hasta);
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("2026-9")]
    [InlineData("09-2026")]
    [InlineData("septiembre")]
    public async Task CA4_MesInvalido_LanzaValidacion(string mes)
    {
        var error = await Assert.ThrowsAsync<ValidacionException>(
            () => Servicio(DateTimeOffset.UtcNow).ConsultarAsync(mes, null, null, null, null, default));

        Assert.Equal("MES_INVALIDO", error.Codigo);
        Assert.Null(repositorio.FiltroRecibido);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public async Task CA4_PaginacionInvalida_LanzaValidacion(int pagina, int tamano)
    {
        var error = await Assert.ThrowsAsync<ValidacionException>(
            () => Servicio(DateTimeOffset.UtcNow).ConsultarAsync("2026-09", null, null, pagina, tamano, default));

        Assert.Equal("PAGINACION_INVALIDA", error.Codigo);
    }

    [Fact]
    public async Task CA4_FiltroConIdentificadorNoPositivo_LanzaValidacion()
    {
        var error = await Assert.ThrowsAsync<ValidacionException>(
            () => Servicio(DateTimeOffset.UtcNow).ConsultarAsync("2026-09", 0, -1, null, null, default));

        Assert.Equal("VALIDACION", error.Codigo);
        Assert.Contains("gestorId", error.Errores.Keys);
        Assert.Contains("ciudadId", error.Errores.Keys);
    }
}
