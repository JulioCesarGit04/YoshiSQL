using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Administracion;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Dominio.Respaldos;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Administracion;

/// <summary>
/// Asistente para restaurar una base de datos desde un archivo .bak del servidor.
/// </summary>
public sealed partial class DialogoDeRestauracionModeloDeVista : ModeloDeVistaBase
{
    private readonly ServidorConectado _servidor;
    private readonly ServicioDeRespaldos _servicioDeRespaldos;
    private readonly IServicioDeDialogos _servicioDeDialogos;
    private readonly IServicioDeErrores _servicioDeErrores;
    private CarpetasDelServidor? _carpetasDelServidor;

    public DialogoDeRestauracionModeloDeVista(
        ServidorConectado servidor,
        ServicioDeRespaldos servicioDeRespaldos,
        IServicioDeDialogos servicioDeDialogos,
        IServicioDeErrores servicioDeErrores)
    {
        _servidor = servidor;
        _servicioDeRespaldos = servicioDeRespaldos;
        _servicioDeDialogos = servicioDeDialogos;
        _servicioDeErrores = servicioDeErrores;
    }

    public ObservableCollection<string> Mensajes { get; } = [];

    public bool RespaldoLeido => InformacionDelRespaldo is not null;

    public string DescripcionDelRespaldo => InformacionDelRespaldo is { } informacion
        ? $"Contiene la base \"{informacion.BaseDeDatosOriginal}\" respaldada el {informacion.FechaDelRespaldo:dd/MM/yyyy HH:mm} ({informacion.Archivos.Count} archivos)."
        : "Escribe la ruta del archivo .bak en el servidor y pulsa \"Leer respaldo\".";

    public string ScriptGenerado => CrearOpciones() is { } opciones ? _servicioDeRespaldos.GenerarScriptDeRestauracion(opciones) : string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LeerRespaldoCommand))]
    public partial string RutaDelArchivo { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RespaldoLeido), nameof(DescripcionDelRespaldo), nameof(ScriptGenerado))]
    [NotifyCanExecuteChangedFor(nameof(RestaurarCommand))]
    public partial InformacionDelRespaldo? InformacionDelRespaldo { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScriptGenerado))]
    [NotifyCanExecuteChangedFor(nameof(RestaurarCommand))]
    public partial string BaseDeDatosDestino { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScriptGenerado))]
    public partial bool Reemplazar { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScriptGenerado))]
    public partial bool CerrarConexionesExistentes { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestaurarCommand), nameof(LeerRespaldoCommand))]
    public partial bool EstaTrabajando { get; private set; }

    [ObservableProperty]
    public partial bool TerminoCorrectamente { get; private set; }

    public async Task InicializarAsync()
    {
        try
        {
            _carpetasDelServidor = await _servicioDeRespaldos.ObtenerCarpetasDelServidorAsync(_servidor, CancellationToken.None);
            RutaDelArchivo = _carpetasDelServidor.Respaldos;
        }
        catch (Exception error)
        {
            Mensajes.Add(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Leer carpetas del servidor")));
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeLeerRespaldo))]
    private async Task LeerRespaldoAsync()
    {
        EstaTrabajando = true;
        Mensajes.Clear();

        try
        {
            InformacionDelRespaldo = await _servicioDeRespaldos.LeerRespaldoAsync(_servidor, RutaDelArchivo.Trim(), CancellationToken.None);
            BaseDeDatosDestino = InformacionDelRespaldo.BaseDeDatosOriginal;
        }
        catch (Exception error)
        {
            InformacionDelRespaldo = null;
            Mensajes.Add(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Leer respaldo")));
        }
        finally
        {
            EstaTrabajando = false;
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeRestaurar))]
    private async Task RestaurarAsync()
    {
        if (Reemplazar && !await ConfirmarReemplazoAsync())
        {
            return;
        }

        EstaTrabajando = true;
        TerminoCorrectamente = false;
        Mensajes.Clear();
        Mensajes.Add("Restaurando...");

        try
        {
            var mensajesDelServidor = await _servicioDeRespaldos.EjecutarAsync(_servidor, ScriptGenerado, CancellationToken.None);
            Mensajes.Clear();

            foreach (var mensaje in mensajesDelServidor)
            {
                Mensajes.Add(mensaje.Texto);
            }

            Mensajes.Add($"Base de datos \"{BaseDeDatosDestino}\" restaurada.");
            TerminoCorrectamente = true;
        }
        catch (Exception error)
        {
            Mensajes.Clear();
            Mensajes.Add(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Restaurar base de datos")));
        }
        finally
        {
            EstaTrabajando = false;
        }
    }

    private bool PuedeLeerRespaldo() => !EstaTrabajando && !string.IsNullOrWhiteSpace(RutaDelArchivo);

    private bool PuedeRestaurar() => !EstaTrabajando && RespaldoLeido && !string.IsNullOrWhiteSpace(BaseDeDatosDestino);

    private Task<bool> ConfirmarReemplazoAsync() =>
        _servicioDeDialogos.ConfirmarAsync(
            "Reemplazar base de datos",
            $"Si la base de datos \"{BaseDeDatosDestino}\" ya existe, se perderán todos sus datos actuales y se reemplazarán por los del respaldo.\n¿Deseas continuar?",
            "Reemplazar",
            "Cancelar");

    private OpcionesDeRestauracion? CrearOpciones() =>
        InformacionDelRespaldo is null || _carpetasDelServidor is null || string.IsNullOrWhiteSpace(BaseDeDatosDestino)
            ? null
            : new OpcionesDeRestauracion(
                RutaDelArchivo.Trim(),
                BaseDeDatosDestino.Trim(),
                Reemplazar,
                CerrarConexionesExistentes,
                InformacionDelRespaldo.Archivos,
                _carpetasDelServidor.Datos,
                _carpetasDelServidor.Registros);

    private ContextoDeError CrearContextoDeError(string accion) =>
        new(accion, _servidor.Perfil.NombreVisible, BaseDeDatosDestino);
}
