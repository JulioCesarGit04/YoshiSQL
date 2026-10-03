using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Respaldos;

namespace YoshiSQL.Aplicacion.Administracion;

public sealed class ServicioDeRespaldos
{
    private readonly IProveedorDeBaseDeDatos _proveedor;
    private readonly TimeProvider _reloj;

    public ServicioDeRespaldos(IProveedorDeBaseDeDatos proveedor, TimeProvider reloj)
    {
        _proveedor = proveedor;
        _reloj = reloj;
    }

    public Task<CarpetasDelServidor> ObtenerCarpetasDelServidorAsync(ServidorConectado servidor, CancellationToken tokenDeCancelacion) =>
        _proveedor.Respaldos.ObtenerCarpetasDelServidorAsync(servidor.DatosDeAcceso, tokenDeCancelacion);

    public Task<InformacionDelRespaldo> LeerRespaldoAsync(ServidorConectado servidor, string rutaDelArchivo, CancellationToken tokenDeCancelacion) =>
        _proveedor.Respaldos.LeerRespaldoAsync(servidor.DatosDeAcceso, rutaDelArchivo, tokenDeCancelacion);

    /// <summary>
    /// Ruta propuesta para un respaldo nuevo, ej. /var/opt/mssql/data/Ventas_2026-10-03_1830.bak
    /// </summary>
    public string SugerirRutaDelRespaldo(string carpetaDeRespaldos, string baseDeDatos)
    {
        var separador = carpetaDeRespaldos.Contains('\\') ? '\\' : '/';
        var marcaDeTiempo = _reloj.GetLocalNow().ToString("yyyy-MM-dd_HHmm");
        return $"{carpetaDeRespaldos.TrimEnd('/', '\\')}{separador}{baseDeDatos}_{marcaDeTiempo}.bak";
    }

    public string GenerarScriptDeRespaldo(OpcionesDeRespaldo opciones) =>
        _proveedor.Respaldos.GenerarScriptDeRespaldo(opciones);

    public string GenerarScriptDeRestauracion(OpcionesDeRestauracion opciones) =>
        _proveedor.Respaldos.GenerarScriptDeRestauracion(opciones);

    /// <summary>
    /// Ejecuta el script de respaldo o restauración desde master.
    /// </summary>
    /// <returns>Los mensajes del servidor, incluido el avance (10 %, 20 %...).</returns>
    public async Task<IReadOnlyList<MensajeDeEjecucion>> EjecutarAsync(
        ServidorConectado servidor,
        string script,
        CancellationToken tokenDeCancelacion)
    {
        var lotes = _proveedor.DivisorDeLotes.DividirEnLotes(script);
        await using var sesion = await _proveedor.Ejecutor.AbrirSesionAsync(
            servidor.DatosDeAcceso, Dominio.Conexiones.PerfilDeConexion.BaseDeDatosDelSistema, tokenDeCancelacion);
        var resultado = await sesion.EjecutarLotesAsync(lotes, tokenDeCancelacion);

        if (resultado.TieneErrores)
        {
            var errores = resultado.Mensajes.Where(mensaje => mensaje.Tipo == TipoDeMensaje.Error).Select(mensaje => mensaje.Texto);
            throw new ErrorDeEjecucion($"La operación no se completó. SQL Server informó:\n{string.Join(Environment.NewLine, errores)}");
        }

        return resultado.Mensajes;
    }
}
