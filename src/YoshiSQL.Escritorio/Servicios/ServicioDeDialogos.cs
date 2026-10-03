using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Escritorio.ModelosDeVista.Conexiones;
using YoshiSQL.Escritorio.Vistas.Comunes;
using YoshiSQL.Escritorio.Vistas.Conexiones;

namespace YoshiSQL.Escritorio.Servicios;

public sealed class ServicioDeDialogos : IServicioDeDialogos
{
    private static readonly FilePickerFileType ArchivosSql = new("Scripts SQL") { Patterns = ["*.sql"] };
    private static readonly FilePickerFileType TodosLosArchivos = new("Todos los archivos") { Patterns = ["*"] };

    private readonly ServicioDeConexiones _servicioDeConexiones;
    private readonly IServicioDeErrores _servicioDeErrores;
    private readonly IServicioDelSistemaOperativo _sistemaOperativo;

    public ServicioDeDialogos(
        ServicioDeConexiones servicioDeConexiones,
        IServicioDeErrores servicioDeErrores,
        IServicioDelSistemaOperativo sistemaOperativo)
    {
        _servicioDeConexiones = servicioDeConexiones;
        _servicioDeErrores = servicioDeErrores;
        _sistemaOperativo = sistemaOperativo;
    }

    public async Task<ServidorConectado?> MostrarDialogoDeConexionAsync(PerfilDeConexion? perfilSugerido = null)
    {
        var modelo = new DialogoDeConexionModeloDeVista(_servicioDeConexiones, _servicioDeErrores);
        await modelo.CargarPerfilesGuardadosAsync(perfilSugerido?.Id);

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

    public async Task<string?> SeleccionarArchivoParaGuardarAsync(string nombreSugerido, TipoDeArchivo tipoDeArchivo)
    {
        var tipoParaElSelector = new FilePickerFileType(tipoDeArchivo.Descripcion) { Patterns = [$"*.{tipoDeArchivo.Extension}"] };

        var archivo = await ObtenerVentanaPrincipal().StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Guardar como {tipoDeArchivo.Descripcion}",
            SuggestedFileName = Path.ChangeExtension(nombreSugerido, tipoDeArchivo.Extension),
            DefaultExtension = tipoDeArchivo.Extension,
            FileTypeChoices = [tipoParaElSelector, TodosLosArchivos]
        });

        return archivo?.TryGetLocalPath();
    }

    public async Task<bool> ConfirmarAsync(string titulo, string mensaje, string textoParaAceptar, string textoParaRechazar)
    {
        var dialogo = new DialogoDeMensaje(
            titulo,
            mensaje,
            [
                new OpcionDeDialogo(textoParaAceptar, true, EsPrincipal: true),
                new OpcionDeDialogo(textoParaRechazar, false, EsCancelar: true)
            ]);

        return await dialogo.ShowDialog<object?>(ObtenerVentanaPrincipal()) is true;
    }

    public async Task MostrarInformacionAsync(string titulo, string mensaje)
    {
        var dialogo = new DialogoDeMensaje(
            titulo,
            mensaje,
            [new OpcionDeDialogo("Aceptar", true, EsPrincipal: true, EsCancelar: true)]);

        await dialogo.ShowDialog<object?>(ObtenerVentanaPrincipal());
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

    public Task MostrarAcercaDeAsync()
    {
        var dialogo = new DialogoAcercaDe(_sistemaOperativo);
        return dialogo.ShowDialog(ObtenerVentanaPrincipal());
    }

    private static Window ObtenerVentanaPrincipal() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
        ?? throw new InvalidOperationException("La ventana principal aún no existe.");
}
