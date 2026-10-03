using System.Collections.ObjectModel;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Sesion;
using YoshiSQL.Escritorio.ModelosDeVista.Resultados;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Editor;

/// <summary>
/// Una pestaña de consulta: su documento, su conexión propia y sus resultados.
/// </summary>
public sealed partial class PestanaDeConsultaModeloDeVista : DocumentoModeloDeVista
{
    private readonly ServicioDeEjecucion _servicioDeEjecucion;
    private readonly ServicioDelExplorador _servicioDelExplorador;
    private readonly IServicioDeErrores _servicioDeErrores;
    private ISesionDeConsulta? _sesion;
    private CancellationTokenSource? _cancelacionDeLaEjecucion;
    private bool _estaCargandoTexto;

    public PestanaDeConsultaModeloDeVista(
        ServidorConectado servidor,
        string baseDeDatos,
        string nombreDelArchivo,
        ServicioDeEjecucion servicioDeEjecucion,
        ServicioDelExplorador servicioDelExplorador,
        IServicioDeErrores servicioDeErrores)
        : base(servidor)
    {
        _servicioDeErrores = servicioDeErrores;
        BaseDeDatosActual = baseDeDatos;
        NombreDelArchivo = nombreDelArchivo;
        _servicioDeEjecucion = servicioDeEjecucion;
        _servicioDelExplorador = servicioDelExplorador;

        Documento.TextChanged += (_, _) => MarcarCambiosSinGuardar();
    }

    public TextDocument Documento { get; } = new();

    public ResultadosModeloDeVista Resultados { get; } = new();

    public ObservableCollection<string> BasesDeDatosDisponibles { get; } = [];

    public override string Titulo => TieneCambiosSinGuardar ? $"{NombreDelArchivo} *" : NombreDelArchivo;

    public override string? InformacionAdicional => RutaDelArchivo;

    /// <summary>
    /// Se dispara después de ejecutar un script que crea, elimina o modifica bases de datos.
    /// </summary>
    public event EventHandler? BasesDeDatosModificadas;

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EjecutarCommand), nameof(CancelarCommand))]
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
            var resultado = await _servicioDeEjecucion.EjecutarAsync(_sesion, fragmento, tokenDeCancelacion);
            MostrarResultado(resultado);

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
        var seleccionEsValida = !Seleccion.EstaVacio
            && Seleccion.Inicio + Seleccion.Longitud <= Documento.TextLength;

        if (!seleccionEsValida)
        {
            return new FragmentoDeCodigo(Documento.Text, LineaInicial: 1);
        }

        var texto = Documento.GetText(Seleccion.Inicio, Seleccion.Longitud);
        var lineaInicial = Documento.GetLineByOffset(Seleccion.Inicio).LineNumber;

        return new FragmentoDeCodigo(texto, lineaInicial);
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

    private void ReemplazarBasesDeDatosDisponibles(IReadOnlyList<string> nombres)
    {
        BasesDeDatosDisponibles.Clear();

        foreach (var nombre in nombres)
        {
            BasesDeDatosDisponibles.Add(nombre);
        }

        AgregarBaseDeDatosSiNoExiste(BaseDeDatosActual);
        OnPropertyChanged(nameof(BaseDeDatosSeleccionada));
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
