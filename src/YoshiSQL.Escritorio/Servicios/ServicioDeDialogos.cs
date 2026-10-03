using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using YoshiSQL.Aplicacion.Administracion;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Escritorio.ModelosDeVista.Administracion;
using YoshiSQL.Escritorio.ModelosDeVista.Conexiones;
using YoshiSQL.Escritorio.Vistas.Administracion;
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
    private readonly ServicioDeRespaldos _servicioDeRespaldos;

    public ServicioDeDialogos(
        ServicioDeConexiones servicioDeConexiones,
        IServicioDeErrores servicioDeErrores,
        IServicioDelSistemaOperativo sistemaOperativo,
        ServicioDeRespaldos servicioDeRespaldos)
    {
        _servicioDeRespaldos = servicioDeRespaldos;
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

        return await dialogo.ShowDialog<object?>(ObtenerVentanaActiva()) is true;
    }

    public async Task MostrarInformacionAsync(string titulo, string mensaje)
    {
        var dialogo = new DialogoDeMensaje(
            titulo,
            mensaje,
            [new OpcionDeDialogo("Aceptar", true, EsPrincipal: true, EsCancelar: true)]);

        await dialogo.ShowDialog<object?>(ObtenerVentanaPrincipal());
    }

    public async Task MostrarRespaldoAsync(ServidorConectado servidor, string baseDeDatos)
    {
        var modelo = new DialogoDeRespaldoModeloDeVista(servidor, baseDeDatos, _servicioDeRespaldos, _servicioDeErrores);
        var dialogo = new DialogoDeRespaldo { DataContext = modelo };
        dialogo.Opened += async (_, _) => await modelo.InicializarAsync();
        await dialogo.ShowDialog(ObtenerVentanaPrincipal());
    }

    public async Task<bool> MostrarRestauracionAsync(ServidorConectado servidor)
    {
        var modelo = new DialogoDeRestauracionModeloDeVista(servidor, _servicioDeRespaldos, this, _servicioDeErrores);
        var dialogo = new DialogoDeRestauracion { DataContext = modelo };
        dialogo.Opened += async (_, _) => await modelo.InicializarAsync();
        await dialogo.ShowDialog(ObtenerVentanaPrincipal());
        return modelo.TerminoCorrectamente;
    }

    /// <summary>
    /// Ventana sobre la cual se abre un diálogo: la que esté activa (puede ser otro diálogo) o la principal.
    /// </summary>
    private static Window ObtenerVentanaActiva()
    {
        var escritorio = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return escritorio?.Windows.LastOrDefault(ventana => ventana.IsActive) ?? ObtenerVentanaPrincipal();
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
