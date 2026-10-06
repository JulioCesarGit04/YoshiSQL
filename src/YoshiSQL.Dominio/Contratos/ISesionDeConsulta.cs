using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Dominio.Contratos;

/// <summary>
/// Conexión propia de una pestaña de consulta. Se mantiene abierta para que
/// USE, tablas temporales y transacciones sigan vivas entre ejecuciones, como en SSMS.
/// </summary>
public interface ISesionDeConsulta : IAsyncDisposable
{
    string BaseDeDatosActual { get; }

    bool EstaEjecutando { get; }

    Task<ResultadoDeEjecucion> EjecutarLotesAsync(
        IReadOnlyList<LoteSql> lotes,
        CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Ejecuta los lotes y además devuelve el plan real de cada instrucción.
    /// </summary>
    Task<ResultadoDeEjecucion> EjecutarConPlanRealAsync(
        IReadOnlyList<LoteSql> lotes,
        CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Ejecuta los lotes con SET STATISTICS IO y TIME activos; las estadísticas llegan como mensajes del servidor.
    /// </summary>
    Task<ResultadoDeEjecucion> EjecutarConEstadisticasAsync(
        IReadOnlyList<LoteSql> lotes,
        CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Pide el plan que usaría el servidor sin ejecutar nada.
    /// </summary>
    /// <returns>Un documento XML por instrucción.</returns>
    Task<IReadOnlyList<string>> ObtenerPlanesEstimadosAsync(
        IReadOnlyList<LoteSql> lotes,
        CancellationToken tokenDeCancelacion);

    Task CambiarBaseDeDatosAsync(string baseDeDatos, CancellationToken tokenDeCancelacion);
}
