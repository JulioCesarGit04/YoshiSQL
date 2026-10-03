using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Scripts;

/// <summary>
/// Scripts que se abren desde el menú contextual del explorador.
/// </summary>
public sealed class ServicioDeGeneracionDeScripts
{
    public const int FilasPorDefectoAlSeleccionar = 1000;

    private readonly IGeneradorDeScripts _generadorDeScripts;
    private readonly IExploradorDeEsquema _exploradorDeEsquema;

    public ServicioDeGeneracionDeScripts(IProveedorDeBaseDeDatos proveedor)
    {
        _generadorDeScripts = proveedor.GeneradorDeScripts;
        _exploradorDeEsquema = proveedor.Explorador;
    }

    public string GenerarSeleccionDeFilas(string baseDeDatos, ObjetoDeEsquema objeto) =>
        _generadorDeScripts.GenerarSeleccionDeFilas(baseDeDatos, objeto, FilasPorDefectoAlSeleccionar);

    public string GenerarCreacionDeBaseDeDatos(string nombreDeLaBaseDeDatos) =>
        _generadorDeScripts.GenerarCreacionDeBaseDeDatos(nombreDeLaBaseDeDatos);

    /// <summary>
    /// Script de ejemplo para crear una tabla nueva, listo para que el usuario lo adapte.
    /// </summary>
    public string GenerarPlantillaDeTablaNueva(string baseDeDatos)
    {
        var tablaDeEjemplo = new Tabla("dbo", "NuevaTabla");
        var columnasDeEjemplo = new[]
        {
            new Columna("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true, Posicion: 1),
            new Columna("Nombre", new TipoDeDato("nvarchar", 100), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2),
            new Columna("FechaDeCreacion", new TipoDeDato("datetime2"), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 3)
        };

        return _generadorDeScripts.GenerarCreacionDeTabla(baseDeDatos, tablaDeEjemplo, columnasDeEjemplo);
    }

    public string GenerarEliminacion(string baseDeDatos, ObjetoDeEsquema objeto) =>
        _generadorDeScripts.GenerarEliminacion(baseDeDatos, objeto);

    public string GenerarEliminacionDeBaseDeDatos(string nombreDeLaBaseDeDatos) =>
        _generadorDeScripts.GenerarEliminacionDeBaseDeDatos(nombreDeLaBaseDeDatos);

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
