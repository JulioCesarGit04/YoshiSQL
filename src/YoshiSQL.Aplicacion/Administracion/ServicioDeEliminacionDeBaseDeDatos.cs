using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Administracion;

/// <summary>
/// Elimina una base de datos como el diálogo "Eliminar objeto" de SSMS.
/// </summary>
public sealed class ServicioDeEliminacionDeBaseDeDatos
{
    // Número de error de SQL Server "No se puede eliminar la base de datos porque está en uso"
    private const string NumeroDeErrorDeBaseEnUso = "Mensaje 3702,";

    private static readonly HashSet<string> BasesDelSistema = new(StringComparer.OrdinalIgnoreCase)
    {
        "master", "model", "msdb", "tempdb"
    };

    private readonly IProveedorDeBaseDeDatos _proveedor;

    public ServicioDeEliminacionDeBaseDeDatos(IProveedorDeBaseDeDatos proveedor)
    {
        _proveedor = proveedor;
    }

    public static bool EsBaseDelSistema(string baseDeDatos) => BasesDelSistema.Contains(baseDeDatos);

    public async Task EliminarAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        OpcionesDeEliminacionDeBaseDeDatos opciones,
        CancellationToken tokenDeCancelacion)
    {
        if (EsBaseDelSistema(baseDeDatos))
        {
            throw new ErrorDeEjecucion($"\"{baseDeDatos}\" es una base de datos del sistema y no se puede eliminar.");
        }

        // Las conexiones que el explorador deja abiertas para reutilizar siguen "dentro" de la base:
        // sin liberarlas, el DROP fallaría por culpa de la propia aplicación
        _proveedor.Ejecutor.LiberarConexionesInactivas();

        var script = _proveedor.GeneradorDeScripts.GenerarEliminacionDeBaseDeDatos(baseDeDatos, opciones);
        var lotes = _proveedor.DivisorDeLotes.DividirEnLotes(script);

        await using var sesion = await _proveedor.Ejecutor.AbrirSesionAsync(
            servidor.DatosDeAcceso, PerfilDeConexion.BaseDeDatosDelSistema, tokenDeCancelacion);
        var resultado = await sesion.EjecutarLotesAsync(lotes, tokenDeCancelacion);

        if (!resultado.TieneErrores)
        {
            return;
        }

        var errores = resultado.Mensajes
            .Where(mensaje => mensaje.Tipo == TipoDeMensaje.Error)
            .Select(mensaje => mensaje.Texto)
            .ToList();
        var sugerencia = !opciones.CerrarConexionesExistentes && errores.Any(error => error.StartsWith(NumeroDeErrorDeBaseEnUso, StringComparison.Ordinal))
            ? "\n\nHay sesiones abiertas en la base. Vuelve a intentarlo marcando \"Cerrar las conexiones existentes\"."
            : string.Empty;

        throw new ErrorDeEjecucion($"No se pudo eliminar la base de datos. SQL Server informó:\n{string.Join(Environment.NewLine, errores)}{sugerencia}");
    }
}
