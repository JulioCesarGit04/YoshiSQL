using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using YoshiSQL.Aplicacion.Administracion;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Preferencias;
using Avalonia.Media;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Escritorio.ModelosDeVista.Administracion;
using YoshiSQL.Escritorio.ModelosDeVista.Conexiones;
using YoshiSQL.Escritorio.Vistas.Administracion;
using YoshiSQL.Escritorio.ModelosDeVista.Preferencias;
using YoshiSQL.Escritorio.Vistas.Preferencias;
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
    private readonly ServicioDePreferencias _servicioDePreferencias;
    private readonly DisposicionDeLaInterfaz _disposicion;

    // Por debajo de esto un diálogo queda inservible; un tamaño guardado menor (archivo editado a mano) se ignora
    private const double LadoMinimoDeUnDialogo = 200;

    public ServicioDeDialogos(
        ServicioDeConexiones servicioDeConexiones,
        IServicioDeErrores servicioDeErrores,
        IServicioDelSistemaOperativo sistemaOperativo,
        ServicioDeRespaldos servicioDeRespaldos,
        ServicioDePreferencias servicioDePreferencias,
        DisposicionDeLaInterfaz disposicion)
    {
        _disposicion = disposicion;
        _servicioDeRespaldos = servicioDeRespaldos;
        _servicioDePreferencias = servicioDePreferencias;
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

    public Task MostrarTextoAsync(string titulo, string texto) =>
        ConTamanoRecordado(new DialogoDeTexto(titulo, texto)).ShowDialog(ObtenerVentanaActiva());

    /// <summary>
    /// Abre el diálogo con el tamaño que el usuario le dejó la última vez y lo recuerda al cerrarlo.
    /// Se guarda en disco junto con la disposición de la ventana principal, al salir de YoshiSQL.
    /// </summary>
    private T ConTamanoRecordado<T>(T dialogo)
        where T : Window
    {
        var nombre = typeof(T).Name;

        if (_disposicion.Dialogos.TryGetValue(nombre, out var tamano)
            && tamano.Ancho >= LadoMinimoDeUnDialogo && tamano.Alto >= LadoMinimoDeUnDialogo)
        {
            dialogo.SizeToContent = SizeToContent.Manual;
            dialogo.Width = tamano.Ancho;
            dialogo.Height = tamano.Alto;
        }

        dialogo.Closing += (_, _) =>
            _disposicion.Dialogos[nombre] = new Dominio.Preferencias.TamanoDeVentana(dialogo.ClientSize.Width, dialogo.ClientSize.Height);

        return dialogo;
    }

    public async Task<bool> EjecutarConProgresoAsync(string titulo, Func<IProgress<string>, CancellationToken, Task> tarea)
    {
        var dialogo = new DialogoDeProgreso(titulo, tarea);
        var llegoAlFinal = await dialogo.ShowDialog<bool>(ObtenerVentanaActiva());

        if (dialogo.Error is { } error)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        }

        return llegoAlFinal;
    }

    public Task<string?> PedirTextoAsync(string titulo, string etiqueta, string valorInicial) =>
        new DialogoDeEntrada(titulo, etiqueta, valorInicial).ShowDialog<string?>(ObtenerVentanaActiva());

    public Task<Aplicacion.Scripts.OpcionesDeExportacion?> PedirOpcionesDeExportacionAsync(string baseDeDatos) =>
        new DialogoDeExportacion(baseDeDatos).ShowDialog<Aplicacion.Scripts.OpcionesDeExportacion?>(ObtenerVentanaActiva());

    public Task<Dominio.Esquema.OpcionesDeEliminacionDeBaseDeDatos?> PedirOpcionesDeEliminacionAsync(string baseDeDatos) =>
        new DialogoDeEliminacionDeBaseDeDatos(baseDeDatos).ShowDialog<Dominio.Esquema.OpcionesDeEliminacionDeBaseDeDatos?>(ObtenerVentanaActiva());

    public Task<ModelosDeVista.Paleta.ComandoDePaleta?> MostrarPaletaDeComandosAsync(IReadOnlyList<ModelosDeVista.Paleta.ComandoDePaleta> comandos)
    {
        var modelo = new ModelosDeVista.Paleta.PaletaDeComandosModeloDeVista(comandos);
        return new DialogoDePaleta(modelo).ShowDialog<ModelosDeVista.Paleta.ComandoDePaleta?>(ObtenerVentanaActiva());
    }

    public Task MostrarPreferenciasAsync()
    {
        var fuentesInstaladas = FontManager.Current.SystemFonts
            .Select(fuente => fuente.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var modelo = new DialogoDePreferenciasModeloDeVista(_servicioDePreferencias, _servicioDeErrores, fuentesInstaladas);
        return new DialogoDePreferencias(modelo).ShowDialog(ObtenerVentanaPrincipal());
    }

    public async Task MostrarRespaldoAsync(ServidorConectado servidor, string baseDeDatos)
    {
        var modelo = new DialogoDeRespaldoModeloDeVista(servidor, baseDeDatos, _servicioDeRespaldos, _servicioDeErrores);
        var dialogo = ConTamanoRecordado(new DialogoDeRespaldo { DataContext = modelo });
        dialogo.Opened += async (_, _) => await modelo.InicializarAsync();
        await dialogo.ShowDialog(ObtenerVentanaPrincipal());
    }

    public async Task<bool> MostrarRestauracionAsync(ServidorConectado servidor)
    {
        var modelo = new DialogoDeRestauracionModeloDeVista(servidor, _servicioDeRespaldos, this, _servicioDeErrores);
        var dialogo = ConTamanoRecordado(new DialogoDeRestauracion { DataContext = modelo });
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
