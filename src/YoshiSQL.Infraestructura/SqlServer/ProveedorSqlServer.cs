using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class ProveedorSqlServer : IProveedorDeBaseDeDatos
{
    public ProveedorSqlServer(
        IExploradorDeEsquema explorador,
        IEjecutorDeConsultas ejecutor,
        IDivisorDeLotes divisorDeLotes,
        IGeneradorDeScripts generadorDeScripts)
    {
        Explorador = explorador;
        Ejecutor = ejecutor;
        DivisorDeLotes = divisorDeLotes;
        GeneradorDeScripts = generadorDeScripts;
    }

    public string NombreDelMotor => "SQL Server";

    public IExploradorDeEsquema Explorador { get; }

    public IEjecutorDeConsultas Ejecutor { get; }

    public IDivisorDeLotes DivisorDeLotes { get; }

    public IGeneradorDeScripts GeneradorDeScripts { get; }

    public async Task ProbarConexionAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion)
    {
        var sesion = await Ejecutor.AbrirSesionAsync(
            datosDeAcceso,
            datosDeAcceso.Perfil.BaseDeDatosPredeterminada,
            tokenDeCancelacion);

        await sesion.DisposeAsync();
    }
}
