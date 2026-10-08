using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Preferencias;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Scripts;

/// <summary>
/// Scripts que se abren desde el menú contextual del explorador.
/// </summary>
public sealed class ServicioDeGeneracionDeScripts
{
    private readonly IGeneradorDeScripts _generadorDeScripts;
    private readonly IExploradorDeEsquema _exploradorDeEsquema;
    private readonly ServicioDePreferencias _servicioDePreferencias;

    public ServicioDeGeneracionDeScripts(IProveedorDeBaseDeDatos proveedor, ServicioDePreferencias servicioDePreferencias)
    {
        _generadorDeScripts = proveedor.GeneradorDeScripts;
        _exploradorDeEsquema = proveedor.Explorador;
        _servicioDePreferencias = servicioDePreferencias;
    }

    /// <summary>Filas que trae "Seleccionar las primeras N filas"; se configura en Preferencias.</summary>
    public int FilasPorDefectoAlSeleccionar => _servicioDePreferencias.Actuales.FilasAlSeleccionar;

    public string GenerarSeleccionDeFilas(string baseDeDatos, ObjetoDeEsquema objeto) =>
        _generadorDeScripts.GenerarSeleccionDeFilas(baseDeDatos, objeto, FilasPorDefectoAlSeleccionar);

    public string GenerarCreacionDeBaseDeDatos(string nombreDeLaBaseDeDatos) =>
        _generadorDeScripts.GenerarCreacionDeBaseDeDatos(nombreDeLaBaseDeDatos);

    public string GenerarEliminacion(string baseDeDatos, ObjetoDeEsquema objeto) =>
        _generadorDeScripts.GenerarEliminacion(baseDeDatos, objeto);

    public string GenerarRenombrado(string baseDeDatos, ObjetoDeEsquema objeto, string nuevoNombre) =>
        _generadorDeScripts.GenerarRenombrado(baseDeDatos, objeto, nuevoNombre);

    public string GenerarConsultaDeFragmentacion(string baseDeDatos, Tabla tabla) =>
        _generadorDeScripts.GenerarConsultaDeFragmentacion(baseDeDatos, tabla);

    public string GenerarMantenimientoDeIndice(string baseDeDatos, Tabla tabla, string nombreDelIndice, bool reconstruir) =>
        _generadorDeScripts.GenerarMantenimientoDeIndice(baseDeDatos, tabla, nombreDelIndice, reconstruir);

    public string GenerarEliminacionDeBaseDeDatos(string nombreDeLaBaseDeDatos) =>
        _generadorDeScripts.GenerarEliminacionDeBaseDeDatos(nombreDeLaBaseDeDatos);

    public string GenerarCreacionDeInicioDeSesion() => _generadorDeScripts.GenerarCreacionDeInicioDeSesion();

    public string GenerarEliminacionDeInicioDeSesion(string nombre) => _generadorDeScripts.GenerarEliminacionDeInicioDeSesion(nombre);

    public string GenerarCreacionDeUsuario(string baseDeDatos) => _generadorDeScripts.GenerarCreacionDeUsuario(baseDeDatos);

    public string GenerarEliminacionDeUsuario(string baseDeDatos, string nombre) =>
        _generadorDeScripts.GenerarEliminacionDeUsuario(baseDeDatos, nombre);

    /// <summary>
    /// Script ALTER con el código actual de una vista, procedimiento o función (opción "Modificar").
    /// </summary>
    public async Task<string> GenerarModificacionAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion)
    {
        var definicion = await ObtenerDefinicionObligatoriaAsync(servidor, baseDeDatos, objeto, tokenDeCancelacion);
        return _generadorDeScripts.GenerarModificacionDesdeDefinicion(baseDeDatos, definicion);
    }

    public async Task<string> GenerarCreacionDeObjetoAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion)
    {
        var definicion = await ObtenerDefinicionObligatoriaAsync(servidor, baseDeDatos, objeto, tokenDeCancelacion);
        return _generadorDeScripts.GenerarCreacionDesdeDefinicion(baseDeDatos, definicion);
    }

    private async Task<string> ObtenerDefinicionObligatoriaAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion)
    {
        var definicion = await _exploradorDeEsquema.ObtenerDefinicionAsync(
            servidor.DatosDeAcceso, baseDeDatos, objeto, tokenDeCancelacion);

        return definicion ?? throw new ErrorDeYoshiSql(
            $"No se puede ver el código de {objeto.NombreCompleto}: el objeto está cifrado o tu usuario no tiene permiso VIEW DEFINITION.");
    }

    /// <summary>Plantilla EXEC de un procedimiento con sus parámetros, para abrir en el editor.</summary>
    public async Task<string> GenerarEjecucionDeProcedimientoAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        ObjetoDeEsquema procedimiento,
        CancellationToken tokenDeCancelacion)
    {
        var parametros = await _exploradorDeEsquema.ObtenerParametrosAsync(servidor.DatosDeAcceso, baseDeDatos, procedimiento, tokenDeCancelacion);
        return _generadorDeScripts.GenerarEjecucionDeProcedimiento(baseDeDatos, procedimiento, parametros);
    }

    /// <summary>Plantilla SELECT/INSERT/UPDATE/DELETE de una tabla o vista para abrir en el editor.</summary>
    public async Task<string> GenerarInstruccionDmlAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        TipoDeScriptDml tipo,
        CancellationToken tokenDeCancelacion)
    {
        var columnas = await _exploradorDeEsquema.ObtenerColumnasAsync(servidor.DatosDeAcceso, baseDeDatos, objeto, tokenDeCancelacion);
        return _generadorDeScripts.GenerarInstruccionDml(baseDeDatos, objeto, columnas, tipo);
    }

    public async Task<string> GenerarCreacionDeTablaAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion)
    {
        var columnas = await _exploradorDeEsquema.ObtenerColumnasAsync(
            servidor.DatosDeAcceso,
            baseDeDatos,
            tabla,
            tokenDeCancelacion);

        return _generadorDeScripts.GenerarCreacionDeTabla(baseDeDatos, tabla, columnas);
    }
}
