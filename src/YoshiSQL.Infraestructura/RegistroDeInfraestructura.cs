using Microsoft.Extensions.DependencyInjection;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Infraestructura.Persistencia;
using YoshiSQL.Infraestructura.Seguridad;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura;

public static class RegistroDeInfraestructura
{
    public static IServiceCollection AgregarInfraestructura(this IServiceCollection servicios)
    {
        servicios.AddSingleton<RutasDeLaAplicacion>();
        servicios.AddSingleton<IRepositorioDeConexiones, RepositorioDeConexionesJson>();
        servicios.AddSingleton<IRepositorioDeScripts, RepositorioDeScriptsEnDisco>();
        servicios.AddSingleton<IAlmacenDeCredenciales, AlmacenDeCredencialesCifrado>();
        servicios.AddSingleton<IRepositorioDeDiagramas, RepositorioDeDiagramasJson>();
        servicios.AddSingleton<IRepositorioDeSesion, RepositorioDeSesionJson>();

        servicios.AgregarProveedorSqlServer();

        return servicios;
    }

    private static void AgregarProveedorSqlServer(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IExploradorDeEsquema, ExploradorDeEsquemaSqlServer>();
        servicios.AddSingleton<IEjecutorDeConsultas, EjecutorDeConsultasSqlServer>();
        servicios.AddSingleton<IDivisorDeLotes, DivisorDeLotesTSql>();
        servicios.AddSingleton<IGeneradorDeScripts, GeneradorDeScriptsSqlServer>();
        servicios.AddSingleton<IFormateadorDeSql, FormateadorDeSqlTSql>();
        servicios.AddSingleton<IProveedorDeBaseDeDatos, ProveedorSqlServer>();
    }
}
