using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Actividad;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Administracion;

public sealed class ServicioDeMonitor
{
    private readonly IMonitorDeActividad _monitor;

    public ServicioDeMonitor(IProveedorDeBaseDeDatos proveedor)
    {
        _monitor = proveedor.Monitor;
    }

    public Task<IReadOnlyList<ProcesoActivo>> ObtenerProcesosAsync(ServidorConectado servidor, CancellationToken tokenDeCancelacion) =>
        _monitor.ObtenerProcesosAsync(servidor.DatosDeAcceso, tokenDeCancelacion);

    public Task TerminarProcesoAsync(ServidorConectado servidor, int idDeSesion, CancellationToken tokenDeCancelacion) =>
        _monitor.TerminarProcesoAsync(servidor.DatosDeAcceso, idDeSesion, tokenDeCancelacion);
}
