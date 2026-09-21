using Seguimiento.Servicios.Excepciones;
using Seguimiento.Servicios.Pacientes;

namespace Seguimiento.Servicios.Tests;

/// <summary>CA-2: el gestor busca al paciente al que le registra el contacto.</summary>
public sealed class CA2_BusquedaDePacientesTests
{
    private sealed class PacienteRepositorioFalso : IPacienteRepositorio
    {
        public string? TextoRecibido { get; private set; }
        public int MaximoRecibido { get; private set; }

        public Task<IReadOnlyList<PacienteResumen>> BuscarActivosAsync(string texto, int maximo, CancellationToken cancelacion)
        {
            TextoRecibido = texto;
            MaximoRecibido = maximo;
            return Task.FromResult<IReadOnlyList<PacienteResumen>>([]);
        }
    }

    private readonly PacienteRepositorioFalso repositorio = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" a ")]
    public async Task CA2_BuscarPacientes_TextoCorto_LanzaValidacion(string? texto)
    {
        var error = await Assert.ThrowsAsync<ValidacionException>(
            () => new PacienteService(repositorio).BuscarAsync(texto, default));

        Assert.Equal("BUSQUEDA_CORTA", error.Codigo);
        Assert.Null(repositorio.TextoRecibido);
    }

    [Fact]
    public async Task CA2_BuscarPacientes_TextoValido_BuscaRecortadoYConLimite()
    {
        await new PacienteService(repositorio).BuscarAsync("  María ", default);

        Assert.Equal("María", repositorio.TextoRecibido);
        Assert.Equal(PacienteService.MaximoResultados, repositorio.MaximoRecibido);
    }
}
