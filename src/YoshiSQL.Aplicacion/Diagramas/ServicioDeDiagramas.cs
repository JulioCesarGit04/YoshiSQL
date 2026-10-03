using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Diagramas;

namespace YoshiSQL.Aplicacion.Diagramas;

public sealed class ServicioDeDiagramas
{
    private readonly IExploradorDeEsquema _exploradorDeEsquema;
    private readonly IRepositorioDeDiagramas _repositorioDeDiagramas;

    public ServicioDeDiagramas(IProveedorDeBaseDeDatos proveedor, IRepositorioDeDiagramas repositorioDeDiagramas)
    {
        _exploradorDeEsquema = proveedor.Explorador;
        _repositorioDeDiagramas = repositorioDeDiagramas;
    }

    /// <summary>
    /// Lee las tablas y relaciones del servidor y les aplica las posiciones guardadas.
    /// Las tablas nuevas quedan sin posición para que se organicen automáticamente.
    /// </summary>
    public async Task<Diagrama> CargarDiagramaAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var columnasPorTabla = await _exploradorDeEsquema.ObtenerColumnasDeTodasLasTablasAsync(
            servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);
        var llavesForaneas = await _exploradorDeEsquema.ObtenerLlavesForaneasAsync(
            servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);
        var disposicionGuardada = await _repositorioDeDiagramas.CargarDisposicionAsync(
            ObtenerClaveDelServidor(servidor), baseDeDatos, tokenDeCancelacion);

        var nodos = columnasPorTabla
            .OrderBy(par => par.Key.NombreCompleto, StringComparer.OrdinalIgnoreCase)
            .Select(par => new NodoDeTabla(par.Key, par.Value)
            {
                Posicion = disposicionGuardada?.ObtenerPosicion(par.Key)
            })
            .ToList();

        var relaciones = llavesForaneas
            .Where(llave => columnasPorTabla.ContainsKey(llave.TablaOrigen) && columnasPorTabla.ContainsKey(llave.TablaDestino))
            .Select(llave => new RelacionEntreTablas(llave))
            .ToList();

        return new Diagrama(baseDeDatos, nodos, relaciones);
    }

    public Task GuardarDisposicionAsync(
        ServidorConectado servidor,
        Diagrama diagrama,
        CancellationToken tokenDeCancelacion) =>
        _repositorioDeDiagramas.GuardarDisposicionAsync(
            ObtenerClaveDelServidor(servidor),
            diagrama.BaseDeDatos,
            DisposicionDeDiagrama.CrearDesde(diagrama.Nodos),
            tokenDeCancelacion);

    private static string ObtenerClaveDelServidor(ServidorConectado servidor) => servidor.Perfil.NombreVisible;
}
