using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Diagramas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Aplicacion.Scripts;
using YoshiSQL.Aplicacion.Sesion;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Escritorio.ModelosDeVista.Administracion;
using YoshiSQL.Escritorio.ModelosDeVista.Diagramas;
using YoshiSQL.Aplicacion.Administracion;
using YoshiSQL.Escritorio.ModelosDeVista.DisenoDeTablas;
using YoshiSQL.Escritorio.ModelosDeVista.EdicionDeFilas;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Aplicacion.Autocompletado;
using YoshiSQL.Escritorio.ModelosDeVista.Editor;
using YoshiSQL.Escritorio.ModelosDeVista.Explorador;
using YoshiSQL.Escritorio.ModelosDeVista.Historial;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista;

public sealed partial class VentanaPrincipalModeloDeVista : ModeloDeVistaBase, IAccionesDelExplorador, IAccionesDelHistorial, IAccionesDelDiseno
{
    private const string PrefijoDeConsultaNueva = "SQLQuery";

    private readonly ServiciosDeConsulta _serviciosDeConsulta;
    private readonly ServicioDelExplorador _servicioDelExplorador;
    private readonly ServicioDeArchivosSql _servicioDeArchivosSql;
    private readonly ServicioDeDiagramas _servicioDeDiagramas;
    private readonly IServicioDeDialogos _servicioDeDialogos;
    private readonly IServicioDeErrores _servicioDeErrores;
    private readonly IServicioDelSistemaOperativo _sistemaOperativo;
    private readonly ServicioDeSesion _servicioDeSesion;
    private readonly HistorialDeConsultas _historialDeConsultas;
    private readonly ServiciosDeDiseno _serviciosDeDiseno;
    private readonly ServicioDeMonitor _servicioDeMonitor;
    private int _contadorDeConsultasNuevas;

    public VentanaPrincipalModeloDeVista(
        ServiciosDeConsulta serviciosDeConsulta,
        ServicioDelExplorador servicioDelExplorador,
        ServicioDeArchivosSql servicioDeArchivosSql,
        ServicioDeGeneracionDeScripts servicioDeGeneracionDeScripts,
        ServicioDeDiagramas servicioDeDiagramas,
        IServicioDeDialogos servicioDeDialogos,
        IServicioDeErrores servicioDeErrores,
        IServicioDelSistemaOperativo sistemaOperativo,
        ServicioDeSesion servicioDeSesion,
        HistorialDeConsultas historialDeConsultas,
        ServiciosDeDiseno serviciosDeDiseno,
        ServicioDeMonitor servicioDeMonitor)
    {
        _servicioDeMonitor = servicioDeMonitor;
        _serviciosDeDiseno = serviciosDeDiseno;
        _historialDeConsultas = historialDeConsultas;
        _servicioDeSesion = servicioDeSesion;
        _servicioDeErrores = servicioDeErrores;
        _sistemaOperativo = sistemaOperativo;
        _servicioDeDiagramas = servicioDeDiagramas;
        _serviciosDeConsulta = serviciosDeConsulta;
        _servicioDelExplorador = servicioDelExplorador;
        _servicioDeArchivosSql = servicioDeArchivosSql;
        _servicioDeDialogos = servicioDeDialogos;

        var fabricaDeNodos = new FabricaDeNodos(servicioDelExplorador, servicioDeGeneracionDeScripts, this, servicioDeErrores);
        Explorador = new ExploradorModeloDeVista(fabricaDeNodos);
        Historial = new HistorialModeloDeVista(historialDeConsultas, this, servicioDeDialogos, sistemaOperativo, servicioDeErrores);
    }

    public ExploradorModeloDeVista Explorador { get; }

    public HistorialModeloDeVista Historial { get; }

    public ObservableCollection<DocumentoModeloDeVista> Documentos { get; } = [];

    public bool HayDocumentosAbiertos => Documentos.Count > 0;

    public bool NoHayDocumentosAbiertos => Documentos.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConsultaSeleccionada), nameof(HayConsultaSeleccionada))]
    public partial DocumentoModeloDeVista? DocumentoSeleccionado { get; set; }

    /// <summary>
    /// La pestaña seleccionada si es una consulta; los botones Ejecutar, Guardar y la lista
    /// de bases de datos solo aplican a consultas.
    /// </summary>
    public PestanaDeConsultaModeloDeVista? ConsultaSeleccionada => DocumentoSeleccionado as PestanaDeConsultaModeloDeVista;

    public bool HayConsultaSeleccionada => ConsultaSeleccionada is not null;

    [RelayCommand]
    private async Task ConectarAsync()
    {
        var servidor = await _servicioDeDialogos.MostrarDialogoDeConexionAsync();

        if (servidor is null)
        {
            return;
        }

        Explorador.AgregarServidor(servidor);

        if (NoHayDocumentosAbiertos)
        {
            await AbrirNuevaConsultaAsync(new ContextoDelNodo(servidor), string.Empty, ejecutarAlAbrir: false);
        }
    }

    [RelayCommand]
    private async Task NuevaConsultaAsync()
    {
        var contexto = await ObtenerOSolicitarContextoAsync();

        if (contexto is not null)
        {
            await AbrirNuevaConsultaAsync(contexto, string.Empty, ejecutarAlAbrir: false);
        }
    }

    [RelayCommand]
    private async Task AbrirArchivoAsync()
    {
        var rutaDelArchivo = await _servicioDeDialogos.SeleccionarArchivoParaAbrirAsync();

        if (rutaDelArchivo is null)
        {
            return;
        }

        var contexto = await ObtenerOSolicitarContextoAsync();

        if (contexto is null)
        {
            return;
        }

        try
        {
            var contenido = await _servicioDeArchivosSql.AbrirAsync(rutaDelArchivo, CancellationToken.None);
            var pestana = await AgregarPestanaAsync(contexto, contenido);
            pestana.MarcarComoGuardado(rutaDelArchivo);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Abrir archivo"));
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (ConsultaSeleccionada is { } pestana)
        {
            await GuardarPestanaAsync(pestana, pedirUbicacion: pestana.RutaDelArchivo is null);
        }
    }

    [RelayCommand]
    private async Task GuardarComoAsync()
    {
        if (ConsultaSeleccionada is { } pestana)
        {
            await GuardarPestanaAsync(pestana, pedirUbicacion: true);
        }
    }

    [RelayCommand]
    private async Task AbrirCarpetaDeRegistrosAsync()
    {
        try
        {
            _sistemaOperativo.AbrirCarpetaDeRegistros();
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Abrir carpeta de registros"));
        }
    }

    [RelayCommand]
    private Task MostrarAcercaDeAsync() => _servicioDeDialogos.MostrarAcercaDeAsync();

    [RelayCommand]
    private Task MostrarPreferenciasAsync() => _servicioDeDialogos.MostrarPreferenciasAsync();

    [RelayCommand]
    private async Task CerrarPestanaAsync(DocumentoModeloDeVista? documento)
    {
        documento ??= DocumentoSeleccionado;

        if (documento is not null && await ConfirmarCierreAsync(documento))
        {
            await QuitarDocumentoAsync(documento);
        }
    }

    /// <summary>
    /// Pregunta por los cambios sin guardar de cada pestaña antes de cerrar la aplicación.
    /// </summary>
    /// <returns>true si la aplicación puede cerrarse.</returns>
    public async Task<bool> PrepararCierreAsync()
    {
        foreach (var documento in Documentos.ToList())
        {
            DocumentoSeleccionado = documento;

            if (!await ConfirmarCierreAsync(documento))
            {
                return false;
            }
        }

        await CerrarSesionCorrectamenteAsync();

        foreach (var documento in Documentos)
        {
            await documento.DisposeAsync();
        }

        return true;
    }

    public async Task AbrirNuevaConsultaAsync(ContextoDelNodo contexto, string textoInicial, bool ejecutarAlAbrir)
    {
        var pestana = await AgregarPestanaAsync(contexto, textoInicial);

        if (ejecutarAlAbrir)
        {
            await pestana.EjecutarCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Abre el diagrama de la base de datos; si ya está abierto, solo lo selecciona.
    /// </summary>
    public async Task AbrirDiagramaAsync(ContextoDelNodo contexto)
    {
        var baseDeDatos = contexto.BaseDeDatosOPredeterminada;
        var diagramaAbierto = Documentos
            .OfType<PestanaDeDiagramaModeloDeVista>()
            .FirstOrDefault(diagrama => diagrama.Servidor == contexto.Servidor && diagrama.BaseDeDatos == baseDeDatos);

        if (diagramaAbierto is not null)
        {
            DocumentoSeleccionado = diagramaAbierto;
            return;
        }

        var diagrama = new PestanaDeDiagramaModeloDeVista(contexto.Servidor, baseDeDatos, _servicioDeDiagramas, _servicioDeErrores);
        AgregarDocumento(diagrama);
        await diagrama.CargarCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Usa el servidor donde se ejecutó la consulta si sigue conectado; si no, el servidor actual.
    /// </summary>
    public async Task AbrirConsultaDelHistorialAsync(ConsultaEjecutada consulta)
    {
        var contexto = Explorador.BuscarContextoDelServidor(consulta.Servidor) ?? await ObtenerOSolicitarContextoAsync();

        if (contexto is not null)
        {
            await AbrirNuevaConsultaAsync(contexto with { BaseDeDatos = consulta.BaseDeDatos }, consulta.Texto, ejecutarAlAbrir: false);
        }
    }

    public async Task AbrirDisenadorDeTablaAsync(ContextoDelNodo contexto, Tabla? tabla)
    {
        var disenador = new PestanaDeDisenoDeTablaModeloDeVista(
            contexto.Servidor, contexto.BaseDeDatosOPredeterminada, tabla, _serviciosDeDiseno, this);

        AgregarDocumento(disenador);
        await disenador.CargarCommand.ExecuteAsync(null);
    }

    public async Task AbrirEdicionDeFilasAsync(ContextoDelNodo contexto, Tabla tabla)
    {
        var edicion = new PestanaDeEdicionDeFilasModeloDeVista(
            contexto.Servidor, contexto.BaseDeDatosOPredeterminada, tabla, _serviciosDeDiseno);

        AgregarDocumento(edicion);
        await edicion.CargarCommand.ExecuteAsync(null);
    }

    public async Task AbrirMonitorDeActividadAsync(ContextoDelNodo contexto)
    {
        var monitorAbierto = Documentos
            .OfType<PestanaDeMonitorModeloDeVista>()
            .FirstOrDefault(monitor => monitor.Servidor == contexto.Servidor);

        if (monitorAbierto is not null)
        {
            DocumentoSeleccionado = monitorAbierto;
            return;
        }

        var monitor = new PestanaDeMonitorModeloDeVista(contexto.Servidor, _servicioDeMonitor, _servicioDeDialogos, _servicioDeErrores);
        AgregarDocumento(monitor);
        await monitor.IniciarAsync();
    }

    public Task MostrarRespaldoAsync(ContextoDelNodo contexto) =>
        _servicioDeDialogos.MostrarRespaldoAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada);

    public async Task MostrarRestauracionAsync(ContextoDelNodo contexto)
    {
        if (await _servicioDeDialogos.MostrarRestauracionAsync(contexto.Servidor))
        {
            await Explorador.ActualizarBasesDeDatosAsync(contexto.Servidor);
        }
    }

    public Task AbrirScriptEnConsultaAsync(ServidorConectado servidor, string baseDeDatos, string script) =>
        AbrirNuevaConsultaAsync(new ContextoDelNodo(servidor, baseDeDatos), script, ejecutarAlAbrir: false);

    public async Task NotificarTablasModificadasAsync(ServidorConectado servidor, string baseDeDatos)
    {
        _serviciosDeConsulta.Autocompletado.InvalidarCatalogo(servidor, baseDeDatos);
        await Explorador.ActualizarTablasAsync(servidor, baseDeDatos);
    }

    public void DesconectarServidor(ContextoDelNodo contexto) => Explorador.QuitarServidor(contexto.Servidor);

    /// <param name="nombreDelArchivo">Nombre a usar; si es nulo se genera uno nuevo (SQLQuery1.sql, SQLQuery2.sql...).</param>
    private async Task<PestanaDeConsultaModeloDeVista> AgregarPestanaAsync(
        ContextoDelNodo contexto,
        string textoInicial,
        string? nombreDelArchivo = null)
    {
        var pestana = new PestanaDeConsultaModeloDeVista(
            contexto.Servidor,
            contexto.BaseDeDatosOPredeterminada,
            nombreDelArchivo ?? GenerarNombreDeConsultaNueva(),
            _serviciosDeConsulta);

        pestana.CargarTexto(textoInicial);
        pestana.BasesDeDatosModificadas += ActualizarBasesDeDatosDelExplorador;
        AgregarDocumento(pestana);

        await pestana.CargarBasesDeDatosAsync();
        return pestana;
    }

    private string GenerarNombreDeConsultaNueva() => $"{PrefijoDeConsultaNueva}{++_contadorDeConsultasNuevas}.sql";

    private void AgregarDocumento(DocumentoModeloDeVista documento)
    {
        Documentos.Add(documento);
        DocumentoSeleccionado = documento;
        NotificarCambioDeDocumentos();
    }

    private async Task QuitarDocumentoAsync(DocumentoModeloDeVista documento)
    {
        var indice = Documentos.IndexOf(documento);

        if (documento is PestanaDeConsultaModeloDeVista pestana)
        {
            pestana.BasesDeDatosModificadas -= ActualizarBasesDeDatosDelExplorador;
        }

        Documentos.Remove(documento);

        if (DocumentoSeleccionado is null || DocumentoSeleccionado == documento)
        {
            DocumentoSeleccionado = Documentos.Count == 0 ? null : Documentos[Math.Min(indice, Documentos.Count - 1)];
        }

        NotificarCambioDeDocumentos();
        await documento.DisposeAsync();
    }

    private async void ActualizarBasesDeDatosDelExplorador(object? remitente, EventArgs argumentos)
    {
        if (remitente is not PestanaDeConsultaModeloDeVista pestana)
        {
            return;
        }

        // Manejador de evento asíncrono: los errores se registran y se avisan en lugar de perderse
        try
        {
            await Explorador.ActualizarBasesDeDatosAsync(pestana.Servidor);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(
                error,
                new ContextoDeError("Actualizar el explorador", pestana.Servidor.Perfil.NombreVisible));
        }
    }

    /// <summary>
    /// Solo las consultas con cambios sin guardar piden confirmación; los diagramas se guardan solos.
    /// </summary>
    private async Task<bool> ConfirmarCierreAsync(DocumentoModeloDeVista documento)
    {
        if (documento.TieneCambiosSinAplicar)
        {
            return await _servicioDeDialogos.ConfirmarAsync(
                "Cambios sin aplicar",
                $"\"{documento.Titulo.TrimEnd(' ', '*')}\" tiene cambios que no se aplicaron. ¿Deseas descartarlos?",
                "Descartar",
                "Cancelar");
        }

        if (documento is not PestanaDeConsultaModeloDeVista { TieneCambiosSinGuardar: true } pestana)
        {
            return true;
        }

        var respuesta = await _servicioDeDialogos.PreguntarSiGuardarCambiosAsync(pestana.NombreDelArchivo);

        return respuesta switch
        {
            RespuestaAlCerrar.Guardar => await GuardarPestanaAsync(pestana, pedirUbicacion: pestana.RutaDelArchivo is null),
            RespuestaAlCerrar.NoGuardar => true,
            _ => false
        };
    }

    /// <returns>true si el archivo quedó guardado.</returns>
    private async Task<bool> GuardarPestanaAsync(PestanaDeConsultaModeloDeVista pestana, bool pedirUbicacion)
    {
        var rutaDelArchivo = pedirUbicacion
            ? await _servicioDeDialogos.SeleccionarArchivoParaGuardarAsync(pestana.NombreDelArchivo, TipoDeArchivo.ScriptSql)
            : pestana.RutaDelArchivo;

        if (rutaDelArchivo is null)
        {
            return false;
        }

        try
        {
            var rutaFinal = await _servicioDeArchivosSql.GuardarAsync(rutaDelArchivo, pestana.Documento.Text, CancellationToken.None);
            pestana.MarcarComoGuardado(rutaFinal);
            return true;
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Guardar archivo"));
            return false;
        }
    }

    /// <summary>
    /// Si no hay ningún servidor conectado, abre la ventana de conexión primero.
    /// </summary>
    private async Task<ContextoDelNodo?> ObtenerOSolicitarContextoAsync()
    {
        if (Explorador.ContextoActual is { } contexto)
        {
            return contexto;
        }

        var servidor = await _servicioDeDialogos.MostrarDialogoDeConexionAsync();

        if (servidor is null)
        {
            return null;
        }

        Explorador.AgregarServidor(servidor);
        return new ContextoDelNodo(servidor);
    }

    private void NotificarCambioDeDocumentos()
    {
        OnPropertyChanged(nameof(HayDocumentosAbiertos));
        OnPropertyChanged(nameof(NoHayDocumentosAbiertos));
    }
}
