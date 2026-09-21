using Microsoft.Extensions.DependencyInjection;
using Seguimiento.Servicios.Catalogos;

namespace Seguimiento.Servicios;

public static class RegistroServicios
{
    public static IServiceCollection AddServicios(this IServiceCollection servicios)
    {
        servicios.AddScoped<CatalogoService>();
        return servicios;
    }
}
