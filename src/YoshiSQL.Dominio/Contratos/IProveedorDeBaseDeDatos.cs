using YoshiSQL.Dominio.Conexiones;

namespace YoshiSQL.Dominio.Contratos;

/// <summary>
/// Punto de entrada a un motor de base de datos. Para soportar otro motor
/// basta con crear una nueva implementación de esta interfaz.
/// </summary>
public interface IProveedorDeBaseDeDatos
{
    string NombreDelMotor { get; }

    IExploradorDeEsquema Explorador { get; }

    IEjecutorDeConsultas Ejecutor { get; }

    IDivisorDeLotes DivisorDeLotes { get; }

    IGeneradorDeScripts GeneradorDeScripts { get; }

    /// <summary>
    /// Verifica que el servidor responda; lanza ErrorDeConexion si no es posible.
    /// </summary>
    Task ProbarConexionAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion);
}
