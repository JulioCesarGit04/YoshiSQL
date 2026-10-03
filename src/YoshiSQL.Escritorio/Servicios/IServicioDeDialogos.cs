using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Conexiones;

namespace YoshiSQL.Escritorio.Servicios;

/// <summary>
/// Ventanas emergentes que los modelos de vista pueden pedir sin conocer Avalonia.
/// </summary>
public interface IServicioDeDialogos
{
    /// <param name="perfilSugerido">Conexión guardada que aparece seleccionada al abrir la ventana.</param>
    Task<ServidorConectado?> MostrarDialogoDeConexionAsync(PerfilDeConexion? perfilSugerido = null);

    Task<bool> ConfirmarAsync(string titulo, string mensaje, string textoParaAceptar, string textoParaRechazar);

    Task MostrarInformacionAsync(string titulo, string mensaje);

    Task<string?> SeleccionarArchivoParaAbrirAsync();

    Task<string?> SeleccionarArchivoParaGuardarAsync(string nombreSugerido);

    Task<RespuestaAlCerrar> PreguntarSiGuardarCambiosAsync(string nombreDelArchivo);

    Task MostrarAcercaDeAsync();
}
