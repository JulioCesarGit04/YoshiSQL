using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using YoshiSQL.Escritorio.ModelosDeVista.Paleta;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Paleta de comandos: se escribe para filtrar, se navega con las flechas y Enter ejecuta.
/// Devuelve el comando elegido o null si se cancela.
/// </summary>
public partial class DialogoDePaleta : Window
{
    private readonly PaletaDeComandosModeloDeVista? _modelo;

    public DialogoDePaleta()
    {
        InitializeComponent();
    }

    public DialogoDePaleta(PaletaDeComandosModeloDeVista modelo)
        : this()
    {
        _modelo = modelo;
        DataContext = modelo;
        Opened += (_, _) => CajaDeBusqueda.Focus();
        AddHandler(KeyDownEvent, AlPresionarTecla, RoutingStrategies.Tunnel);
    }

    private void AlPresionarTecla(object? remitente, KeyEventArgs argumentos)
    {
        switch (argumentos.Key)
        {
            case Key.Escape:
                Close(null);
                argumentos.Handled = true;
                break;
            case Key.Enter:
                Close(_modelo?.Seleccionado);
                argumentos.Handled = true;
                break;
            case Key.Down:
                _modelo?.MoverSeleccion(1);
                argumentos.Handled = true;
                break;
            case Key.Up:
                _modelo?.MoverSeleccion(-1);
                argumentos.Handled = true;
                break;
        }
    }

    private void AlHacerDobleClic(object? remitente, TappedEventArgs argumentos) => Close(_modelo?.Seleccionado);
}
