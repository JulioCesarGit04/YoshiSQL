using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Preferencias;
using YoshiSQL.Escritorio.Controles;
using YoshiSQL.Escritorio.ModelosDeVista;
using YoshiSQL.Escritorio.Servicios;
using YoshiSQL.Escritorio.Vistas;
using YoshiSQL.Escritorio.Vistas.Comunes;

namespace YoshiSQL.Escritorio;

public partial class App : Application
{
    public override void Initialize()
    {
        TraduccionDeAvaloniaEdit.Aplicar();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime escritorio)
        {
            var bienvenida = new PantallaDeBienvenida();
            bienvenida.Show();

            var proveedorDeServicios = ContenedorDeDependencias.Construir();
            RegistrarCapturaDeErroresDeLaInterfaz(proveedorDeServicios.GetRequiredService<IServicioDeErrores>());
            AplicarPreferenciasGuardadas(proveedorDeServicios);

            var modeloPrincipal = proveedorDeServicios.GetRequiredService<VentanaPrincipalModeloDeVista>();
            var ventana = new VentanaPrincipal { DataContext = modeloPrincipal };
            escritorio.MainWindow = ventana;
            escritorio.Exit += (_, _) => proveedorDeServicios.Dispose();

            MostrarVentanaTrasLaBienvenida(ventana, bienvenida);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Deja ver la pantalla de bienvenida un instante y luego la cierra (la ventana principal ya está debajo).
    /// </summary>
    private static void MostrarVentanaTrasLaBienvenida(Window ventana, Window bienvenida)
    {
        var temporizador = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.6) };
        temporizador.Tick += (_, _) =>
        {
            temporizador.Stop();
            ventana.Activate();
            bienvenida.Close();
        };
        temporizador.Start();
    }


    /// <summary>
    /// Las preferencias (tema, letra del editor) se aplican antes de mostrar la ventana
    /// para que no se vea un cambio de colores al iniciar.
    /// </summary>
    private static void AplicarPreferenciasGuardadas(IServiceProvider proveedorDeServicios)
    {
        var servicioDePreferencias = proveedorDeServicios.GetRequiredService<ServicioDePreferencias>();

        try
        {
            // Se lee en un hilo aparte para no bloquear el hilo de la interfaz esperando su propio contexto
            Task.Run(() => servicioDePreferencias.CargarAsync(CancellationToken.None)).GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            proveedorDeServicios.GetRequiredService<IServicioDeErrores>()
                .RegistrarYDescribir(error, new ContextoDeError("Cargar preferencias"));
        }

        AplicadorDePreferencias.Aplicar(servicioDePreferencias.Actuales);

        // Crear el aplicador lo suscribe a los cambios que el usuario haga desde Preferencias
        proveedorDeServicios.GetRequiredService<AplicadorDePreferencias>();
    }

    /// <summary>
    /// Cualquier error no controlado en la interfaz se registra y se avisa al usuario,
    /// y la aplicación sigue funcionando en lugar de cerrarse.
    /// </summary>
    private static void RegistrarCapturaDeErroresDeLaInterfaz(IServicioDeErrores servicioDeErrores)
    {
        Dispatcher.UIThread.UnhandledException += (_, argumentos) =>
        {
            argumentos.Handled = true;
            _ = servicioDeErrores.RegistrarYMostrarAsync(argumentos.Exception, new ContextoDeError("Operación de la interfaz"));
        };
    }
}
