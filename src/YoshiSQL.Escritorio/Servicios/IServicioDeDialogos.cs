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

    /// <summary>Muestra un texto largo (propiedades, XML, JSON) en una ventana desplazable.</summary>
    Task MostrarTextoAsync(string titulo, string texto);

    Task<string?> SeleccionarArchivoParaAbrirAsync();

    Task<string?> SeleccionarArchivoParaGuardarAsync(string nombreSugerido, TipoDeArchivo tipoDeArchivo);

    Task<RespuestaAlCerrar> PreguntarSiGuardarCambiosAsync(string nombreDelArchivo);

    Task MostrarAcercaDeAsync();

    Task MostrarPreferenciasAsync();

    Task MostrarRespaldoAsync(ServidorConectado servidor, string baseDeDatos);

    /// <returns>Verdadero si se restauró una base de datos.</returns>
    Task<bool> MostrarRestauracionAsync(ServidorConectado servidor);
}
