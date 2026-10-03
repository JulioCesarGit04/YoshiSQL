using YoshiSQL.Dominio.Conexiones;

namespace YoshiSQL.Dominio.Contratos;

public interface IEjecutorDeConsultas
{
    Task<ISesionDeConsulta> AbrirSesionAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);
}
