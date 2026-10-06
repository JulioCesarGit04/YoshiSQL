using System.Reflection;
using Avalonia.Controls;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Pantalla de bienvenida que se muestra brevemente mientras la aplicación arranca.
/// </summary>
public partial class PantallaDeBienvenida : Window
{
    public PantallaDeBienvenida()
    {
        InitializeComponent();
        TextoDeVersion.Text = $"v{ObtenerVersion()}";
    }

    private static string ObtenerVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? string.Empty : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
