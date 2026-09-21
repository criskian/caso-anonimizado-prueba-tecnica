using Microsoft.Extensions.DependencyInjection;
using Seguimiento.Servicios.Catalogos;

namespace Seguimiento.Datos;

public static class RegistroDatos
{
    public static IServiceCollection AddDatos(this IServiceCollection servicios, string cadenaConexion)
    {
        servicios.AddSingleton(new FabricaConexiones(cadenaConexion));
        servicios.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
        return servicios;
    }
}
