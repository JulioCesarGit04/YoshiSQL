using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Preferencias;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.EdicionDeFilas;

/// <summary>
/// "Editar las primeras N filas": leer filas de una tabla y guardar los cambios hechos en la grilla.
/// La cantidad de filas se configura en Preferencias.
/// </summary>
public sealed class ServicioDeEdicionDeFilas
{
    private readonly IProveedorDeBaseDeDatos _proveedor;
    private readonly ServicioDePreferencias _servicioDePreferencias;

    public ServicioDeEdicionDeFilas(IProveedorDeBaseDeDatos proveedor, ServicioDePreferencias servicioDePreferencias)
    {
        _proveedor = proveedor;
        _servicioDePreferencias = servicioDePreferencias;
    }

    public int CantidadDeFilasAEditar => _servicioDePreferencias.Actuales.FilasAlEditar;

    public async Task<DatosParaEditar> CargarAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion,
        string? filtroWhere = null,
        string? ordenarPor = null)
    {
        var columnas = await _proveedor.Explorador.ObtenerColumnasAsync(servidor.DatosDeAcceso, baseDeDatos, tabla, tokenDeCancelacion);
        var consulta = _proveedor.GeneradorDeScripts.GenerarSeleccionDeFilas(baseDeDatos, tabla, CantidadDeFilasAEditar, filtroWhere, ordenarPor);

        await using var sesion = await _proveedor.Ejecutor.AbrirSesionAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);
        var resultado = await sesion.EjecutarLotesAsync(_proveedor.DivisorDeLotes.DividirEnLotes(consulta), tokenDeCancelacion);

        if (resultado.TieneErrores || resultado.ConjuntosDeResultados.Count == 0)
        {
            var primerError = resultado.Mensajes.FirstOrDefault(mensaje => mensaje.Tipo == Dominio.Consultas.TipoDeMensaje.Error)?.Texto;
            throw new ErrorDeEjecucion($"No se pudieron leer las filas de {tabla.NombreCompleto}. {primerError}");
        }

        return new DatosParaEditar(columnas.OrderBy(columna => columna.Posicion).ToList(), resultado.ConjuntosDeResultados[0].Filas);
    }

    /// <returns>Cantidad de filas afectadas.</returns>
    public Task<int> GuardarCambiosAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        Tabla tabla,
        IReadOnlyList<Columna> columnas,
        IReadOnlyList<CambioDeFila> cambios,
        CancellationToken tokenDeCancelacion)
    {
        var comandos = _proveedor.GeneradorDeScripts.GenerarComandosDeEdicion(tabla, columnas, cambios);
        return _proveedor.Ejecutor.EjecutarEnTransaccionAsync(servidor.DatosDeAcceso, baseDeDatos, comandos, tokenDeCancelacion);
    }
}
