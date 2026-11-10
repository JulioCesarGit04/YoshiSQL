using Avalonia.Controls;
using Avalonia.Interactivity;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Ventana que acompaña a una tarea larga (por ejemplo, exportar una base): muestra por dónde va y permite cancelarla.
/// Se cierra sola al terminar y devuelve verdadero si la tarea llegó al final.
/// </summary>
public partial class DialogoDeProgreso : Window
{
    private readonly CancellationTokenSource _cancelacion = new();
    private readonly Func<IProgress<string>, CancellationToken, Task>? _tarea;
    private bool _termino;

    public DialogoDeProgreso()
    {
        InitializeComponent();
    }

    public DialogoDeProgreso(string titulo, Func<IProgress<string>, CancellationToken, Task> tarea)
        : this()
    {
        Title = titulo;
        _tarea = tarea;
        Opened += async (_, _) => await EjecutarAsync();
        Closing += AlIntentarCerrar;
    }

    /// <summary>El error que detuvo la tarea, si lo hubo; quien abrió la ventana decide cómo mostrarlo.</summary>
    public Exception? Error { get; private set; }

    private async Task EjecutarAsync()
    {
        if (_tarea is not { } tarea)
        {
            return;
        }

        // Progress entrega cada aviso en el hilo de la interfaz, aunque la tarea corra en otro
        var avance = new Progress<string>(texto => Detalle.Text = texto);
        var llegoAlFinal = false;

        try
        {
            // Fuera del hilo de la interfaz: la tarea escribe miles de líneas y la ventana debe seguir respondiendo
            await Task.Run(() => tarea(avance, _cancelacion.Token));
            llegoAlFinal = true;
        }
        catch (OperationCanceledException)
        {
            // El usuario pulsó Cancelar
        }
        catch (Exception error)
        {
            Error = error;
        }
        finally
        {
            _termino = true;
            _cancelacion.Dispose();
            Close(llegoAlFinal);
        }
    }

    private void AlCancelar(object? remitente, RoutedEventArgs argumentos) => PedirCancelacion();

    // Cerrar con la X equivale a cancelar: la ventana se cierra recién cuando la tarea se detuvo
    private void AlIntentarCerrar(object? remitente, WindowClosingEventArgs argumentos)
    {
        if (_termino)
        {
            return;
        }

        argumentos.Cancel = true;
        PedirCancelacion();
    }

    private void PedirCancelacion()
    {
        if (_termino)
        {
            return;
        }

        BotonDeCancelar.IsEnabled = false;
        Detalle.Text = "Cancelando...";
        _cancelacion.Cancel();
    }
}
