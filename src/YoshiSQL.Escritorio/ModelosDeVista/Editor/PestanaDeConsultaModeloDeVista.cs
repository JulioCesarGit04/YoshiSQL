using System.Collections.ObjectModel;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Autocompletado;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Dominio.Autocompletado;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Sesion;
using YoshiSQL.Escritorio.Controles;
using YoshiSQL.Escritorio.ModelosDeVista.Resultados;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Editor;

/// <summary>
/// Una pestaña de consulta: su documento, su conexión propia y sus resultados.
/// </summary>
public sealed partial class PestanaDeConsultaModeloDeVista : DocumentoModeloDeVista, IProveedorDeSugerencias
{
    private readonly ServicioDeEjecucion _servicioDeEjecucion;
    private readonly ServicioDelExplorador _servicioDelExplorador;
    private readonly ServicioDeFormatoSql _servicioDeFormato;
    private readonly ServicioDeAutocompletado _servicioDeAutocompletado;
    private readonly IServicioDeErrores _servicioDeErrores;
    private ISesionDeConsulta? _sesion;
    private CancellationTokenSource? _cancelacionDeLaEjecucion;
    private bool _estaCargandoTexto;

    public PestanaDeConsultaModeloDeVista(
        ServidorConectado servidor,
        string baseDeDatos,
        string nombreDelArchivo,
        ServiciosDeConsulta servicios)
        : base(servidor)
    {
        _servicioDeEjecucion = servicios.Ejecucion;
        _servicioDelExplorador = servicios.Explorador;
        _servicioDeFormato = servicios.Formato;
        _servicioDeAutocompletado = servicios.Autocompletado;
        _servicioDeErrores = servicios.Errores;
        BaseDeDatosActual = baseDeDatos;
        NombreDelArchivo = nombreDelArchivo;

        // La lista nace con la base actual para que la lista desplegable siempre la encuentre seleccionada
        BasesDeDatosDisponibles.Add(baseDeDatos);
        Resultados = new ResultadosModeloDeVista(servicios.Exportacion, servicios.SistemaOperativo);

        Documento.TextChanged += (_, _) => MarcarCambiosSinGuardar();
    }

    public TextDocument Documento { get; } = new();

    public ResultadosModeloDeVista Resultados { get; }

    public ObservableCollection<string> BasesDeDatosDisponibles { get; } = [];

    public override string Titulo => TieneCambiosSinGuardar ? $"{NombreDelArchivo} *" : NombreDelArchivo;

    public override string? InformacionAdicional => RutaDelArchivo;

    /// <summary>
    /// Se dispara después de ejecutar un script que crea, elimina o modifica bases de datos.
    /// </summary>
    public event EventHandler? BasesDeDatosModificadas;

    /// <summary>
    /// Pide a la vista abrir el panel de búsqueda; el argumento indica si también se quiere reemplazar.
    /// </summary>
    public event EventHandler<bool>? BusquedaSolicitada;

    /// <summary>Pide al editor comentar, descomentar o alternar el comentario de la selección.</summary>
    public event EventHandler<AccionDeComentario>? ComentarioSolicitado;

    /// <summary>Pide al editor convertir la selección a mayúsculas (true) o minúsculas (false).</summary>
    public event EventHandler<bool>? CambioDeCapitalizacionSolicitado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InformacionAdicional))]
    public partial string? RutaDelArchivo { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titulo))]
    public partial string NombreDelArchivo { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Titulo))]
    public partial bool TieneCambiosSinGuardar { get; private set; }

    [ObservableProperty]
    public partial RangoDeTexto Seleccion { get; set; }

    /// <summary>
    /// Si está activo, cada ejecución devuelve también el plan real (como "Incluir plan real" en SSMS).
    /// </summary>
    [ObservableProperty]
    public partial bool IncluirPlanReal { get; set; }

    /// <summary>Si está activo, cada ejecución incluye SET STATISTICS IO y TIME (las estadísticas salen en Mensajes).</summary>
    [ObservableProperty]
    public partial bool IncluirEstadisticas { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EjecutarCommand), nameof(CancelarCommand), nameof(MostrarPlanEstimadoCommand))]
    public partial bool EstaEjecutando { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BaseDeDatosSeleccionada))]
    public partial string BaseDeDatosActual { get; private set; }

    [ObservableProperty]
    public partial string DuracionVisible { get; private set; } = "00:00:00";

    [ObservableProperty]
    public partial int TotalDeFilas { get; private set; }

    /// <summary>
    /// Base elegida en la lista desplegable. Ignora los valores vacíos que envía
    /// la lista mientras se recarga, para no perder la base actual.
    /// </summary>
    public string? BaseDeDatosSeleccionada
    {
        get => BaseDeDatosActual;
        set
        {
            if (string.IsNullOrEmpty(value) || value == BaseDeDatosActual)
            {
                return;
            }

            BaseDeDatosActual = value;
            _ = CambiarBaseDeDatosEnLaSesionAsync(value);
        }
    }

    public async Task CargarBasesDeDatosAsync()
    {
        try
        {
            var basesDeDatos = await _servicioDelExplorador.ObtenerBasesDeDatosAsync(Servidor, CancellationToken.None);
            var nombresDisponibles = basesDeDatos
                .Where(baseDeDatos => baseDeDatos.EstaEnLinea)
                .Select(baseDeDatos => baseDeDatos.Nombre)
                .ToList();

            ReemplazarBasesDeDatosDisponibles(nombresDisponibles);
        }
        // Si falla, se registra y la lista muestra solo la base actual; la pestaña sigue siendo usable
        catch (Exception error)
        {
            _servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Cargar la lista de bases de datos"));
            ReemplazarBasesDeDatosDisponibles([BaseDeDatosActual]);
        }
    }

    public void CargarTexto(string texto)
    {
        _estaCargandoTexto = true;
        Documento.Text = texto;
        _estaCargandoTexto = false;
    }

    public bool EstaVacia => RutaDelArchivo is null && !TieneCambiosSinGuardar && Documento.TextLength == 0;

    /// <summary>
    /// Se guarda el texto solo si no está en un archivo o si tiene cambios; si no, basta con la ruta.
    /// </summary>
    public override PestanaGuardada CrearPestanaGuardada()
    {
        var debeGuardarElTexto = TieneCambiosSinGuardar || RutaDelArchivo is null;

        return new PestanaGuardada(
            TipoDePestana.Consulta,
            Servidor.Perfil.Id,
            BaseDeDatosActual,
            NombreDelArchivo,
            RutaDelArchivo,
            debeGuardarElTexto ? Documento.Text : null,
            TieneCambiosSinGuardar);
    }

    public void RestaurarEstadoDelArchivo(string? rutaDelArchivo, bool tieneCambiosSinGuardar)
    {
        RutaDelArchivo = rutaDelArchivo;
        TieneCambiosSinGuardar = tieneCambiosSinGuardar;
    }

    public void MarcarComoGuardado(string rutaDelArchivo)
    {
        RutaDelArchivo = rutaDelArchivo;
        NombreDelArchivo = Path.GetFileName(rutaDelArchivo);
        TieneCambiosSinGuardar = false;
    }

    [RelayCommand(CanExecute = nameof(PuedeEjecutar))]
    private async Task EjecutarAsync()
    {
        var fragmento = ObtenerFragmentoAEjecutar();
        _cancelacionDeLaEjecucion = new CancellationTokenSource();
        var tokenDeCancelacion = _cancelacionDeLaEjecucion.Token;

        EstaEjecutando = true;
        TextoDeEstado = "Ejecutando consulta...";
        Resultados.Limpiar();

        try
        {
            _sesion ??= await _servicioDeEjecucion.AbrirSesionAsync(Servidor, BaseDeDatosActual, tokenDeCancelacion);
            var resultado = await _servicioDeEjecucion.EjecutarAsync(Servidor, _sesion, fragmento, IncluirPlanReal, IncluirEstadisticas, tokenDeCancelacion);
            MostrarResultado(resultado);

            if (_servicioDeEjecucion.InterpretarPlanReal(resultado) is { } planReal)
            {
                Resultados.MostrarPlan(planReal, seleccionarPestana: false);
            }

            if (DetectorDeCambiosDeEsquema.ModificaTablasOVistas(fragmento.Texto))
            {
                _servicioDeAutocompletado.InvalidarCatalogo(Servidor, BaseDeDatosActual);
            }

            if (DetectorDeCambiosDeEsquema.ModificaBasesDeDatos(fragmento.Texto))
            {
                await CargarBasesDeDatosAsync();
                BasesDeDatosModificadas?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            Resultados.MostrarError("La consulta fue cancelada por el usuario.");
            TextoDeEstado = "Consulta cancelada.";
        }
        // El error se registra y se muestra en la pestaña "Mensajes" en lugar de cerrar la aplicación
        catch (Exception error)
        {
            Resultados.MostrarError(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Ejecutar consulta")));
            TextoDeEstado = "La consulta no se pudo ejecutar.";
        }
        finally
        {
            EstaEjecutando = false;
            _cancelacionDeLaEjecucion.Dispose();
            _cancelacionDeLaEjecucion = null;
        }
    }

    private bool PuedeEjecutar() => !EstaEjecutando;

    [RelayCommand(CanExecute = nameof(PuedeEjecutar))]
    private async Task MostrarPlanEstimadoAsync()
    {
        var fragmento = ObtenerFragmentoAEjecutar();
        EstaEjecutando = true;
        TextoDeEstado = "Calculando el plan estimado...";
        Resultados.Limpiar();

        try
        {
            _sesion ??= await _servicioDeEjecucion.AbrirSesionAsync(Servidor, BaseDeDatosActual, CancellationToken.None);
            var plan = await _servicioDeEjecucion.ObtenerPlanEstimadoAsync(_sesion, fragmento, CancellationToken.None);
            Resultados.MostrarPlan(plan, seleccionarPestana: true);
            TextoDeEstado = "Plan estimado listo.";
        }
        catch (Exception error)
        {
            Resultados.MostrarError(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Mostrar plan estimado")));
            TextoDeEstado = "No se pudo obtener el plan.";
        }
        finally
        {
            EstaEjecutando = false;
        }
    }

    [RelayCommand]
    private void ConmutarPlanReal()
    {
        IncluirPlanReal = !IncluirPlanReal;
        TextoDeEstado = IncluirPlanReal ? "Se incluirá el plan real en la próxima ejecución." : "Plan real desactivado.";
    }

    partial void OnIncluirEstadisticasChanged(bool value) =>
        TextoDeEstado = value
            ? "Se incluirán las estadísticas de IO y tiempo (pestaña Mensajes)."
            : "Estadísticas desactivadas.";

    /// <summary>
    /// Formatea el texto subrayado o, si no hay selección, todo el editor. Se puede deshacer con Ctrl+Z.
    /// </summary>
    [RelayCommand]
    private void Formatear()
    {
        var (inicio, longitud) = ObtenerRangoAProcesar();

        try
        {
            var codigoFormateado = _servicioDeFormato.Formatear(Documento.GetText(inicio, longitud));
            Documento.Replace(inicio, longitud, codigoFormateado);
            TextoDeEstado = "Código formateado. Puedes deshacerlo con Ctrl+Z.";
        }
        catch (Exception error)
        {
            Resultados.MostrarError(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Formatear SQL")));
            TextoDeEstado = "No se pudo formatear el código.";
        }
    }

    public async Task<IReadOnlyList<Sugerencia>> ObtenerSugerenciasAsync(string textoCompleto, int posicionDelCursor)
    {
        try
        {
            return await _servicioDeAutocompletado.ObtenerSugerenciasAsync(
                Servidor, BaseDeDatosActual, textoCompleto, posicionDelCursor, CancellationToken.None);
        }
        // Un fallo del autocompletado nunca debe interrumpir la escritura: se registra y no se sugiere nada
        catch (Exception error)
        {
            _servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Autocompletar"));
            return [];
        }
    }

    [RelayCommand]
    private void Buscar() => BusquedaSolicitada?.Invoke(this, false);

    [RelayCommand]
    private void Reemplazar() => BusquedaSolicitada?.Invoke(this, true);

    [RelayCommand]
    private void AlternarComentario() => ComentarioSolicitado?.Invoke(this, AccionDeComentario.Alternar);

    [RelayCommand]
    private void Comentar() => ComentarioSolicitado?.Invoke(this, AccionDeComentario.Comentar);

    [RelayCommand]
    private void Descomentar() => ComentarioSolicitado?.Invoke(this, AccionDeComentario.Descomentar);

    [RelayCommand]
    private void ConvertirAMayusculas() => CambioDeCapitalizacionSolicitado?.Invoke(this, true);

    [RelayCommand]
    private void ConvertirAMinusculas() => CambioDeCapitalizacionSolicitado?.Invoke(this, false);

    [RelayCommand]
    private void ConmutarResultadosEnTexto() => Resultados.MostrarComoTexto = !Resultados.MostrarComoTexto;

    [RelayCommand(CanExecute = nameof(EstaEjecutando))]
    private void Cancelar() => _cancelacionDeLaEjecucion?.Cancel();

    public override async ValueTask DisposeAsync()
    {
        _cancelacionDeLaEjecucion?.Cancel();

        if (_sesion is not null)
        {
            await _sesion.DisposeAsync();
            _sesion = null;
        }
    }

    /// <summary>
    /// Si hay texto subrayado se ejecuta solo eso; si no, todo el editor.
    /// </summary>
    private FragmentoDeCodigo ObtenerFragmentoAEjecutar()
    {
        var (inicio, longitud) = ObtenerRangoAProcesar();
        var lineaInicial = Documento.GetLineByOffset(inicio).LineNumber;

        return new FragmentoDeCodigo(Documento.GetText(inicio, longitud), lineaInicial);
    }

    /// <summary>
    /// La selección actual si es válida; si no, todo el documento.
    /// </summary>
    private (int Inicio, int Longitud) ObtenerRangoAProcesar()
    {
        var seleccionEsValida = !Seleccion.EstaVacio
            && Seleccion.Inicio + Seleccion.Longitud <= Documento.TextLength;

        return seleccionEsValida ? (Seleccion.Inicio, Seleccion.Longitud) : (0, Documento.TextLength);
    }

    private void MostrarResultado(ResultadoDeEjecucion resultado)
    {
        Resultados.Mostrar(resultado);
        DuracionVisible = resultado.Duracion.ToString(@"hh\:mm\:ss");
        TotalDeFilas = resultado.TotalDeFilas;
        TextoDeEstado = DescribirEstado(resultado.Estado);

        // Un USE dentro del script cambia la base de datos de la pestaña, como en SSMS
        if (!string.IsNullOrEmpty(resultado.BaseDeDatosAlTerminar))
        {
            AgregarBaseDeDatosSiNoExiste(resultado.BaseDeDatosAlTerminar);
            BaseDeDatosActual = resultado.BaseDeDatosAlTerminar;
        }
    }

    private async Task CambiarBaseDeDatosEnLaSesionAsync(string baseDeDatos)
    {
        if (_sesion is null)
        {
            return;
        }

        try
        {
            await _sesion.CambiarBaseDeDatosAsync(baseDeDatos, CancellationToken.None);
        }
        catch (Exception error)
        {
            Resultados.MostrarError(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Cambiar de base de datos")));
            BaseDeDatosActual = _sesion.BaseDeDatosActual;
        }
    }

    /// <summary>
    /// Actualiza la lista sin vaciarla: si la base seleccionada desapareciera aunque sea un instante,
    /// la lista desplegable perdería la selección.
    /// </summary>
    private void ReemplazarBasesDeDatosDisponibles(IReadOnlyList<string> nombres)
    {
        IReadOnlyList<string> nombresFinales = nombres.Contains(BaseDeDatosActual) ? nombres : [.. nombres, BaseDeDatosActual];

        foreach (var nombreSobrante in BasesDeDatosDisponibles.Except(nombresFinales).ToList())
        {
            BasesDeDatosDisponibles.Remove(nombreSobrante);
        }

        for (var posicion = 0; posicion < nombresFinales.Count; posicion++)
        {
            var nombre = nombresFinales[posicion];
            var posicionActual = BasesDeDatosDisponibles.IndexOf(nombre);

            if (posicionActual < 0)
            {
                BasesDeDatosDisponibles.Insert(posicion, nombre);
            }
            else if (posicionActual != posicion)
            {
                BasesDeDatosDisponibles.Move(posicionActual, posicion);
            }
        }
    }

    private void AgregarBaseDeDatosSiNoExiste(string baseDeDatos)
    {
        if (!BasesDeDatosDisponibles.Contains(baseDeDatos))
        {
            BasesDeDatosDisponibles.Add(baseDeDatos);
        }
    }

    private ContextoDeError CrearContextoDeError(string accion) =>
        new(accion, Servidor.Perfil.NombreVisible, BaseDeDatosActual);

    private void MarcarCambiosSinGuardar()
    {
        if (!_estaCargandoTexto)
        {
            TieneCambiosSinGuardar = true;
        }
    }

    private static string DescribirEstado(EstadoDeEjecucion estado) => estado switch
    {
        EstadoDeEjecucion.Completada => "Consulta ejecutada correctamente.",
        EstadoDeEjecucion.CompletadaConErrores => "Consulta completada con errores.",
        EstadoDeEjecucion.Cancelada => "Consulta cancelada.",
        EstadoDeEjecucion.Fallida => "Se perdió la conexión con el servidor.",
        _ => "Ejecutando consulta..."
    };
}
