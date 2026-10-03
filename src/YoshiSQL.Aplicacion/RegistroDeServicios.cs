using Microsoft.Extensions.DependencyInjection;
using YoshiSQL.Aplicacion.Autocompletado;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Diagramas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Aplicacion.Scripts;
using YoshiSQL.Aplicacion.Sesion;

namespace YoshiSQL.Aplicacion;

public static class RegistroDeServicios
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddSingleton<RegistroDeErrores>();
        servicios.AddSingleton<ServicioDeConexiones>();
        servicios.AddSingleton<ServicioDelExplorador>();
        servicios.AddSingleton<HistorialDeConsultas>();
        servicios.AddSingleton<ServicioDeEjecucion>();
        servicios.AddSingleton<ServicioDeFormatoSql>();
        servicios.AddSingleton<ServicioDeExportacion>();
        servicios.AddSingleton<ServicioDeAutocompletado>();
        servicios.AddSingleton<ServicioDeArchivosSql>();
        servicios.AddSingleton<ServicioDeGeneracionDeScripts>();
        servicios.AddSingleton<ServicioDeDiagramas>();
        servicios.AddSingleton<ServicioDeSesion>();

        return servicios;
    }
}
