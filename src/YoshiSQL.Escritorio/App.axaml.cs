using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Escritorio.ModelosDeVista;
using YoshiSQL.Escritorio.Servicios;
using YoshiSQL.Escritorio.Vistas;

namespace YoshiSQL.Escritorio;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime escritorio)
        {
            var proveedorDeServicios = ContenedorDeDependencias.Construir();
            RegistrarCapturaDeErroresDeLaInterfaz(proveedorDeServicios.GetRequiredService<IServicioDeErrores>());

            var modeloPrincipal = proveedorDeServicios.GetRequiredService<VentanaPrincipalModeloDeVista>();
            escritorio.MainWindow = new VentanaPrincipal { DataContext = modeloPrincipal };
            escritorio.Exit += (_, _) => proveedorDeServicios.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
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
