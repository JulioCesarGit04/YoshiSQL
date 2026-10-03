using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Respaldos;

namespace YoshiSQL.Dominio.Contratos;

public interface IAdministradorDeRespaldos
{
    Task<CarpetasDelServidor> ObtenerCarpetasDelServidorAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Lee qué base de datos contiene un archivo .bak y qué archivos hay que ubicar al restaurarlo.
    /// </summary>
    Task<InformacionDelRespaldo> LeerRespaldoAsync(DatosDeAcceso datosDeAcceso, string rutaDelArchivo, CancellationToken tokenDeCancelacion);

    string GenerarScriptDeRespaldo(OpcionesDeRespaldo opciones);

    string GenerarScriptDeRestauracion(OpcionesDeRestauracion opciones);
}
