using Microsoft.Extensions.DependencyInjection;
using Seguimiento.Servicios.Catalogos;
using Seguimiento.Servicios.Contactos;
using Seguimiento.Servicios.Pacientes;

namespace Seguimiento.Servicios;

public static class RegistroServicios
{
    public static IServiceCollection AddServicios(this IServiceCollection servicios)
    {
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddScoped<CatalogoService>();
        servicios.AddScoped<PacienteService>();
        servicios.AddScoped<ContactoService>();
        servicios.AddScoped<ContactosDelMesService>();
        return servicios;
    }
}
