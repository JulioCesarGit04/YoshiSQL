using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Escritorio.ModelosDeVista.Conexiones;
using YoshiSQL.Escritorio.Vistas.Comunes;
using YoshiSQL.Escritorio.Vistas.Conexiones;

namespace YoshiSQL.Escritorio.Servicios;

public sealed class ServicioDeDialogos : IServicioDeDialogos
{
    private static readonly FilePickerFileType ArchivosSql = new("Scripts SQL") { Patterns = ["*.sql"] };
    private static readonly FilePickerFileType TodosLosArchivos = new("Todos los archivos") { Patterns = ["*"] };

    private readonly ServicioDeConexiones _servicioDeConexiones;

    public ServicioDeDialogos(ServicioDeConexiones servicioDeConexiones)
    {
        _servicioDeConexiones = servicioDeConexiones;
    }

    public async Task<ServidorConectado?> MostrarDialogoDeConexionAsync()
    {
        var modelo = new DialogoDeConexionModeloDeVista(_servicioDeConexiones);
        await modelo.CargarPerfilesGuardadosAsync();

        var dialogo = new DialogoDeConexion(modelo);
        return await dialogo.ShowDialog<ServidorConectado?>(ObtenerVentanaPrincipal());
    }

    public async Task<string?> SeleccionarArchivoParaAbrirAsync()
    {
        var archivos = await ObtenerVentanaPrincipal().StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Abrir script",
            AllowMultiple = false,
            FileTypeFilter = [ArchivosSql, TodosLosArchivos]
        });

        return archivos.Count > 0 ? archivos[0].TryGetLocalPath() : null;
    }

    public async Task<string?> SeleccionarArchivoParaGuardarAsync(string nombreSugerido)
    {
        var archivo = await ObtenerVentanaPrincipal().StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar script",
            SuggestedFileName = nombreSugerido,
            DefaultExtension = "sql",
            FileTypeChoices = [ArchivosSql, TodosLosArchivos]
        });

        return archivo?.TryGetLocalPath();
    }

    public async Task<RespuestaAlCerrar> PreguntarSiGuardarCambiosAsync(string nombreDelArchivo)
    {
        var dialogo = new DialogoDeMensaje(
            "YoshiSQL",
            $"¿Deseas guardar los cambios de \"{nombreDelArchivo}\"?",
            [
                new OpcionDeDialogo("Guardar", RespuestaAlCerrar.Guardar, EsPrincipal: true),
                new OpcionDeDialogo("No guardar", RespuestaAlCerrar.NoGuardar),
                new OpcionDeDialogo("Cancelar", RespuestaAlCerrar.Cancelar, EsCancelar: true)
            ]);

        var respuesta = await dialogo.ShowDialog<object?>(ObtenerVentanaPrincipal());
        return respuesta is RespuestaAlCerrar respuestaElegida ? respuestaElegida : RespuestaAlCerrar.Cancelar;
    }

    public async Task MostrarErrorAsync(string mensaje)
    {
        var dialogo = new DialogoDeMensaje(
            "Error",
            mensaje,
            [new OpcionDeDialogo("Aceptar", true, EsPrincipal: true, EsCancelar: true)]);

        await dialogo.ShowDialog<object?>(ObtenerVentanaPrincipal());
    }

    private static Window ObtenerVentanaPrincipal() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
        ?? throw new InvalidOperationException("La ventana principal aún no existe.");
}
