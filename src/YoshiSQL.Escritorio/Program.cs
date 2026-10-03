using Avalonia;

namespace YoshiSQL.Escritorio;

internal static class Program
{
    [STAThread]
    public static void Main(string[] argumentos) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(argumentos);

    // Avalonia exige este nombre exacto para que funcione el previsualizador de diseño
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
