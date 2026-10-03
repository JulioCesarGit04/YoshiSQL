using Avalonia.Controls;
using Avalonia.Input;
using YoshiSQL.Escritorio.ModelosDeVista.Historial;

namespace YoshiSQL.Escritorio.Vistas.Historial;

public partial class PanelDelHistorial : UserControl
{
    public PanelDelHistorial()
    {
        InitializeComponent();
    }

    private void AlHacerDobleClicEnEntrada(object? remitente, TappedEventArgs argumentos)
    {
        var entrada = (DataContext as HistorialModeloDeVista)?.EntradaSeleccionada;
        entrada?.AbrirCommand.Execute(null);
    }
}
