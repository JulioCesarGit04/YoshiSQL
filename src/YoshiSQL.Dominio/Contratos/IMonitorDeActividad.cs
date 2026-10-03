using YoshiSQL.Dominio.Actividad;
using YoshiSQL.Dominio.Conexiones;

namespace YoshiSQL.Dominio.Contratos;

public interface IMonitorDeActividad
{
    /// <summary>
    /// Sesiones de usuario conectadas, sin incluir la del propio monitor.
    /// </summary>
    Task<IReadOnlyList<ProcesoActivo>> ObtenerProcesosAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Termina la sesión (KILL). Su transacción en curso se deshace.
    /// </summary>
    Task TerminarProcesoAsync(DatosDeAcceso datosDeAcceso, int idDeSesion, CancellationToken tokenDeCancelacion);
}
