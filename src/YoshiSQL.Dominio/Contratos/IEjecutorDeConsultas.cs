using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Edicion;

namespace YoshiSQL.Dominio.Contratos;

public interface IEjecutorDeConsultas
{
    Task<ISesionDeConsulta> AbrirSesionAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Ejecuta los comandos en una sola transacción: si uno falla, no se aplica ninguno.
    /// </summary>
    /// <returns>Total de filas afectadas.</returns>
    Task<int> EjecutarEnTransaccionAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        IReadOnlyList<ComandoSql> comandos,
        CancellationToken tokenDeCancelacion);
}
