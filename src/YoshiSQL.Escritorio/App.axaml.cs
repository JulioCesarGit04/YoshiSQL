using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using YoshiSQL.Escritorio.ModelosDeVista;
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

            escritorio.MainWindow = new VentanaPrincipal
            {
                DataContext = proveedorDeServicios.GetRequiredService<VentanaPrincipalModeloDeVista>()
            };
            escritorio.Exit += (_, _) => proveedorDeServicios.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
