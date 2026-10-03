using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class EjecutorDeConsultasSqlServer : IEjecutorDeConsultas
{
    public async Task<ISesionDeConsulta> AbrirSesionAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var cadenaDeConexion = ConstructorDeCadenaDeConexion.Construir(datosDeAcceso, baseDeDatos, usarPoolDeConexiones: false);
        return await SesionDeConsultaSqlServer.AbrirAsync(cadenaDeConexion, tokenDeCancelacion);
    }
}
