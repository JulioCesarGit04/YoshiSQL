using Microsoft.Extensions.DependencyInjection;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Diagramas;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Aplicacion.Scripts;

namespace YoshiSQL.Aplicacion;

public static class RegistroDeServicios
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddSingleton<ServicioDeConexiones>();
        servicios.AddSingleton<ServicioDelExplorador>();
        servicios.AddSingleton<HistorialDeConsultas>();
        servicios.AddSingleton<ServicioDeEjecucion>();
        servicios.AddSingleton<ServicioDeArchivosSql>();
        servicios.AddSingleton<ServicioDeGeneracionDeScripts>();
        servicios.AddSingleton<ServicioDeDiagramas>();

        return servicios;
    }
}
