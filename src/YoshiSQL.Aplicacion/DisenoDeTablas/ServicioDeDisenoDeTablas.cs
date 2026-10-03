using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Diseno;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.DisenoDeTablas;

/// <summary>
/// Crear tablas y modificar su estructura desde el diseñador visual.
/// </summary>
public sealed class ServicioDeDisenoDeTablas
{
    private readonly IProveedorDeBaseDeDatos _proveedor;

    public ServicioDeDisenoDeTablas(IProveedorDeBaseDeDatos proveedor)
    {
        _proveedor = proveedor;
    }

    public IReadOnlyList<string> TiposDeDatoSugeridos => _proveedor.TiposDeDatoSugeridos;

    public async Task<DefinicionDeTabla> CargarAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion)
    {
        var columnas = await _proveedor.Explorador.ObtenerColumnasAsync(servidor.DatosDeAcceso, baseDeDatos, tabla, tokenDeCancelacion);
        var indices = await _proveedor.Explorador.ObtenerIndicesAsync(servidor.DatosDeAcceso, baseDeDatos, tabla, tokenDeCancelacion);

        return new DefinicionDeTabla(
            tabla.Esquema,
            tabla.Nombre,
            columnas.OrderBy(columna => columna.Posicion).Select(DefinicionDeColumna.DesdeColumnaExistente).ToList(),
            indices.FirstOrDefault(indice => indice.EsLlavePrimaria)?.Nombre);
    }

    public IReadOnlyList<string> Validar(DefinicionDeTabla nueva, DefinicionDeTabla? original) =>
        ValidadorDeDefinicionDeTabla.Validar(nueva, original);

    public bool HayCambios(DefinicionDeTabla nueva, DefinicionDeTabla? original) =>
        original is null || CambiosDeTabla.Calcular(original, nueva).HayCambios;

    public string GenerarScript(string baseDeDatos, DefinicionDeTabla nueva, DefinicionDeTabla? original) =>
        _proveedor.GeneradorDeScripts.GenerarCambiosDeTabla(baseDeDatos, original, nueva);

    /// <summary>
    /// Ejecuta el script de cambios. Si SQL Server rechaza cualquier instrucción, la transacción
    /// del script deshace todo y se informa el motivo.
    /// </summary>
    public async Task AplicarAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        DefinicionDeTabla nueva,
        DefinicionDeTabla? original,
        CancellationToken tokenDeCancelacion)
    {
        var errores = Validar(nueva, original);

        if (errores.Count > 0)
        {
            throw new ErrorDeYoshiSql(string.Join(Environment.NewLine, errores));
        }

        var lotes = _proveedor.DivisorDeLotes.DividirEnLotes(GenerarScript(baseDeDatos, nueva, original));
        await using var sesion = await _proveedor.Ejecutor.AbrirSesionAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);
        var resultado = await sesion.EjecutarLotesAsync(lotes, tokenDeCancelacion);

        if (resultado.TieneErrores)
        {
            var mensajesDeError = resultado.Mensajes
                .Where(mensaje => mensaje.Tipo == Dominio.Consultas.TipoDeMensaje.Error)
                .Select(mensaje => mensaje.Texto);

            throw new ErrorDeEjecucion($"No se aplicó ningún cambio. SQL Server informó:\n{string.Join(Environment.NewLine, mensajesDeError)}");
        }
    }
}
