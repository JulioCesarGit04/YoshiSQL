using Avalonia;
using Serilog;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Escritorio.Registro;
using YoshiSQL.Infraestructura.Persistencia;

namespace YoshiSQL.Escritorio;

internal static class Program
{
    private const int CodigoDeSalidaConError = 1;

    [STAThread]
    public static int Main(string[] argumentos)
    {
        var rutas = new RutasDeLaAplicacion();

        // Se crea antes que el registrador para que la carpeta nazca privada (permisos 700)
        rutas.AsegurarQueExistaLaCarpetaDeRegistros();
        Log.Logger = ConfiguracionDelRegistro.CrearRegistrador(rutas.CarpetaDeRegistros);
        RegistrarCapturaDeErroresFueraDeLaInterfaz();

        try
        {
            Log.Information("YoshiSQL iniciado · {Entorno}", InformacionDelEntorno.Describir());
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(argumentos);
        }
        catch (Exception error)
        {
            Log.Fatal(error, "YoshiSQL se cerró por un error que no se pudo recuperar");
            return CodigoDeSalidaConError;
        }
        finally
        {
            Log.Information("YoshiSQL cerrado");
            Log.CloseAndFlush();
        }
    }

    // Avalonia exige este nombre exacto para que funcione el previsualizador de diseño
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Red de seguridad para errores de hilos y tareas en segundo plano. Los errores de la
    /// interfaz se atrapan aparte (en App) para poder mostrar un aviso y seguir funcionando.
    /// </summary>
    private static void RegistrarCapturaDeErroresFueraDeLaInterfaz()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, argumentos) =>
        {
            Log.Fatal(argumentos.ExceptionObject as Exception, "Error no controlado en un hilo de fondo");
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (_, argumentos) =>
        {
            Log.Error(argumentos.Exception, "Error no controlado en una tarea en segundo plano");
            argumentos.SetObserved();
        };
    }
}
