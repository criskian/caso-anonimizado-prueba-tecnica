using Microsoft.Extensions.DependencyInjection;
using Seguimiento.Servicios.Catalogos;
using Seguimiento.Servicios.Contactos;
using Seguimiento.Servicios.Pacientes;

namespace Seguimiento.Datos;

public static class RegistroDatos
{
    public static IServiceCollection AddDatos(this IServiceCollection servicios, string cadenaConexion)
    {
        servicios.AddSingleton(new FabricaConexiones(cadenaConexion));
        servicios.AddScoped<ICatalogoRepositorio, CatalogoRepositorio>();
        servicios.AddScoped<IPacienteRepositorio, PacienteRepositorio>();
        servicios.AddScoped<IContactoRepositorio, ContactoRepositorio>();
        servicios.AddScoped<IContactosDelMesRepositorio, ContactosDelMesRepositorio>();
        return servicios;
    }
}
