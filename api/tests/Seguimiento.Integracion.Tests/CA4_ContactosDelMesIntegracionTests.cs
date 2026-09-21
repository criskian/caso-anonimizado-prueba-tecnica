namespace Seguimiento.Integracion.Tests;

/// <summary>
/// CA-4: dada la vista de contactos del mes, cuando la coordinadora filtra por gestor y
/// ciudad, entonces solo se muestran los contactos que cumplen ambos filtros.
/// El filtro vive en SQL, así que se prueba contra SQL Server real. Cada prueba usa su
/// propio mes y sus propios gestores y ciudades para no depender de las demás.
/// </summary>
[Collection(ColeccionBaseDeDatos.Nombre)]
public sealed class CA4_ContactosDelMesIntegracionTests(BaseDeDatosFixture baseDeDatos)
{
    private readonly Escenario escenario = new(baseDeDatos);

    [Fact]
    public async Task CA4_FiltroGestorYCiudad_SoloDevuelveContactosQueCumplenAmbos()
    {
        var (gestorA, gestorB) = (await escenario.NuevoGestorAsync(), await escenario.NuevoGestorAsync());
        var (ciudadX, ciudadY) = (await escenario.NuevaCiudadAsync(), await escenario.NuevaCiudadAsync());
        var (pacienteX, pacienteY) = (await escenario.NuevoPacienteAsync(ciudadX), await escenario.NuevoPacienteAsync(ciudadY));

        var aX1 = await escenario.RegistrarAsync(gestorA, pacienteX, Escenario.Local(2025, 3, 5));
        var aX2 = await escenario.RegistrarAsync(gestorA, pacienteX, Escenario.Local(2025, 3, 20));
        var aY = await escenario.RegistrarAsync(gestorA, pacienteY, Escenario.Local(2025, 3, 6));
        var bX = await escenario.RegistrarAsync(gestorB, pacienteX, Escenario.Local(2025, 3, 7));
        await escenario.RegistrarAsync(gestorB, pacienteY, Escenario.Local(2025, 3, 8));

        var servicio = escenario.ContactosDelMes;
        var ambos = await servicio.ConsultarAsync("2025-03", gestorA, ciudadX, null, null, default);
        var soloGestor = await servicio.ConsultarAsync("2025-03", gestorA, null, null, null, default);
        var soloCiudad = await servicio.ConsultarAsync("2025-03", null, ciudadX, null, null, default);

        // Con los dos filtros: solo los de A en X, del más reciente al más antiguo.
        Assert.Equal([aX2, aX1], ambos.Items.Select(c => c.Id));
        Assert.Equal(2, ambos.Total);
        Assert.All(ambos.Items, c => Assert.True(c.GestorId == gestorA && c.CiudadId == ciudadX));

        // Cada filtro por separado también funciona.
        Assert.Equal(new[] { aX1, aX2, aY }.Order(), soloGestor.Items.Select(c => c.Id).Order());
        Assert.Equal(new[] { aX1, aX2, bX }.Order(), soloCiudad.Items.Select(c => c.Id).Order());
    }

    [Fact]
    public async Task CA4_SinFiltros_DevuelveTodosLosContactosDelMes()
    {
        var gestor = await escenario.NuevoGestorAsync();
        var paciente = await escenario.NuevoPacienteAsync(await escenario.NuevaCiudadAsync());
        var ids = new[]
        {
            await escenario.RegistrarAsync(gestor, paciente, Escenario.Local(2025, 5, 1, 0, 0)),
            await escenario.RegistrarAsync(gestor, paciente, Escenario.Local(2025, 5, 15)),
            await escenario.RegistrarAsync(gestor, paciente, Escenario.Local(2025, 5, 31, 23, 59)),
        };

        var pagina = await escenario.ContactosDelMes.ConsultarAsync("2025-05", null, null, null, null, default);

        Assert.Equal(ids.Order(), pagina.Items.Select(c => c.Id).Order());
        Assert.Equal(3, pagina.Total);
    }

    [Fact]
    public async Task CA4_ContactoDeOtroMes_NoAparece()
    {
        var gestor = await escenario.NuevoGestorAsync();
        var paciente = await escenario.NuevoPacienteAsync(await escenario.NuevaCiudadAsync());
        await escenario.RegistrarAsync(gestor, paciente, Escenario.Local(2025, 6, 30, 12));
        var deJulio = await escenario.RegistrarAsync(gestor, paciente, Escenario.Local(2025, 7, 15, 12));
        await escenario.RegistrarAsync(gestor, paciente, Escenario.Local(2025, 8, 1, 12));

        var pagina = await escenario.ContactosDelMes.ConsultarAsync("2025-07", gestor, null, null, null, default);

        Assert.Equal(deJulio, Assert.Single(pagina.Items).Id);
    }

    [Fact]
    public async Task CA4_ContactoUltimaNocheDelMesEnHoraLocal_QuedaEnEseMes()
    {
        var gestor = await escenario.NuevoGestorAsync();
        var paciente = await escenario.NuevoPacienteAsync(await escenario.NuevaCiudadAsync());

        // 31 de agosto a las 22:00 en hora local = 1 de septiembre a las 03:00 UTC (H-13).
        var ultimaNocheAgosto = new DateTimeOffset(2025, 9, 1, 3, 0, 0, TimeSpan.Zero);
        // 1 de septiembre a las 00:30 en hora local = 1 de septiembre a las 05:30 UTC.
        var primeraHoraSeptiembre = Escenario.Local(2025, 9, 1, 0, 30);
        var deAgosto = await escenario.RegistrarAsync(gestor, paciente, ultimaNocheAgosto);
        var deSeptiembre = await escenario.RegistrarAsync(gestor, paciente, primeraHoraSeptiembre);

        var agosto = await escenario.ContactosDelMes.ConsultarAsync("2025-08", gestor, null, null, null, default);
        var septiembre = await escenario.ContactosDelMes.ConsultarAsync("2025-09", gestor, null, null, null, default);

        Assert.Equal(deAgosto, Assert.Single(agosto.Items).Id);
        Assert.Equal(deSeptiembre, Assert.Single(septiembre.Items).Id);
    }

    [Fact]
    public async Task CA4_Paginacion_DevuelveLaPaginaPedidaYElTotal()
    {
        var gestor = await escenario.NuevoGestorAsync();
        var paciente = await escenario.NuevoPacienteAsync(await escenario.NuevaCiudadAsync());
        var ids = new List<long>();
        for (var dia = 1; dia <= 5; dia++)
        {
            ids.Add(await escenario.RegistrarAsync(gestor, paciente, Escenario.Local(2025, 10, dia)));
        }

        var servicio = escenario.ContactosDelMes;
        var primera = await servicio.ConsultarAsync("2025-10", gestor, null, 1, 2, default);
        var ultima = await servicio.ConsultarAsync("2025-10", gestor, null, 3, 2, default);
        var fueraDeRango = await servicio.ConsultarAsync("2025-10", gestor, null, 4, 2, default);

        Assert.Equal([ids[4], ids[3]], primera.Items.Select(c => c.Id));
        Assert.Equal([ids[0]], ultima.Items.Select(c => c.Id));
        Assert.Empty(fueraDeRango.Items);
        Assert.All(new[] { primera, ultima, fueraDeRango }, p => Assert.Equal(5, p.Total));
    }
}
