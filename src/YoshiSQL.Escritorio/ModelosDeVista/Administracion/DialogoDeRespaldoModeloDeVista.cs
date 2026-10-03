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
/// Asistente para respaldar una base de datos en un archivo .bak del servidor.
/// </summary>
public sealed partial class DialogoDeRespaldoModeloDeVista : ModeloDeVistaBase
{
    private readonly ServidorConectado _servidor;
    private readonly ServicioDeRespaldos _servicioDeRespaldos;
    private readonly IServicioDeErrores _servicioDeErrores;

    public DialogoDeRespaldoModeloDeVista(
        ServidorConectado servidor,
        string baseDeDatos,
        ServicioDeRespaldos servicioDeRespaldos,
        IServicioDeErrores servicioDeErrores)
    {
        _servidor = servidor;
        BaseDeDatos = baseDeDatos;
        _servicioDeRespaldos = servicioDeRespaldos;
        _servicioDeErrores = servicioDeErrores;
    }

    public string BaseDeDatos { get; }

    public ObservableCollection<string> Mensajes { get; } = [];

    public string ScriptGenerado => _servicioDeRespaldos.GenerarScriptDeRespaldo(CrearOpciones());

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScriptGenerado))]
    [NotifyCanExecuteChangedFor(nameof(RespaldarCommand))]
    public partial string RutaDelArchivo { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScriptGenerado))]
    public partial bool SoloCopia { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScriptGenerado))]
    public partial bool Comprimir { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScriptGenerado))]
    public partial bool Verificar { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RespaldarCommand))]
    public partial bool EstaTrabajando { get; private set; }

    [ObservableProperty]
    public partial bool TerminoCorrectamente { get; private set; }

    public async Task InicializarAsync()
    {
        try
        {
            var carpetas = await _servicioDeRespaldos.ObtenerCarpetasDelServidorAsync(_servidor, CancellationToken.None);
            RutaDelArchivo = _servicioDeRespaldos.SugerirRutaDelRespaldo(carpetas.Respaldos, BaseDeDatos);
        }
        catch (Exception error)
        {
            Mensajes.Add(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Leer carpetas del servidor")));
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeRespaldar))]
    private async Task RespaldarAsync()
    {
        EstaTrabajando = true;
        TerminoCorrectamente = false;
        Mensajes.Clear();
        Mensajes.Add("Creando el respaldo...");

        try
        {
            var mensajesDelServidor = await _servicioDeRespaldos.EjecutarAsync(_servidor, ScriptGenerado, CancellationToken.None);
            Mensajes.Clear();

            foreach (var mensaje in mensajesDelServidor)
            {
                Mensajes.Add(mensaje.Texto);
            }

            Mensajes.Add($"Respaldo creado en {RutaDelArchivo}");
            TerminoCorrectamente = true;
        }
        catch (Exception error)
        {
            Mensajes.Clear();
            Mensajes.Add(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Respaldar base de datos")));
        }
        finally
        {
            EstaTrabajando = false;
        }
    }

    private bool PuedeRespaldar() => !EstaTrabajando && !string.IsNullOrWhiteSpace(RutaDelArchivo);

    private OpcionesDeRespaldo CrearOpciones() =>
        new(BaseDeDatos, RutaDelArchivo.Trim(), SoloCopia, Comprimir, Verificar);

    private ContextoDeError CrearContextoDeError(string accion) =>
        new(accion, _servidor.Perfil.NombreVisible, BaseDeDatos);
}
