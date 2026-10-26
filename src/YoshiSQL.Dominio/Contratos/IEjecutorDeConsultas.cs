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

    /// <summary>
    /// Cierra las conexiones inactivas que la aplicación guarda para reutilizar (las del explorador).
    /// Siguen abiertas en el servidor y, por ejemplo, impiden eliminar la base en la que estaban.
    /// </summary>
    void LiberarConexionesInactivas();
}
