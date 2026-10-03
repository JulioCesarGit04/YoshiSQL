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

    Task CambiarBaseDeDatosAsync(string baseDeDatos, CancellationToken tokenDeCancelacion);
}
