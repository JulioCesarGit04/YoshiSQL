using YoshiSQL.Aplicacion.Conexiones;

namespace YoshiSQL.Escritorio.Servicios;

/// <summary>
/// Ventanas emergentes que los modelos de vista pueden pedir sin conocer Avalonia.
/// </summary>
public interface IServicioDeDialogos
{
    Task<ServidorConectado?> MostrarDialogoDeConexionAsync();

    Task<string?> SeleccionarArchivoParaAbrirAsync();

    Task<string?> SeleccionarArchivoParaGuardarAsync(string nombreSugerido);

    Task<RespuestaAlCerrar> PreguntarSiGuardarCambiosAsync(string nombreDelArchivo);

    Task MostrarErrorAsync(string mensaje);
}
