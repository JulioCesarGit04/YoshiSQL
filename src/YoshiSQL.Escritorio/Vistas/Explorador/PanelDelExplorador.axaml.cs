using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using YoshiSQL.Escritorio.ModelosDeVista.Explorador;

namespace YoshiSQL.Escritorio.Vistas.Explorador;

public partial class PanelDelExplorador : UserControl
{
    public PanelDelExplorador()
    {
        InitializeComponent();
    }

    // Los nodos sin acciones (columnas, índices) no deben mostrar un menú vacío
    private void AlAbrirMenuContextual(object? remitente, CancelEventArgs argumentos)
    {
        if (remitente is ContextMenu menu && menu.ItemCount == 0)
        {
            argumentos.Cancel = true;
        }
    }

    private void AlHacerDobleClicEnNodo(object? remitente, TappedEventArgs argumentos)
    {
        var comando = (DataContext as ExploradorModeloDeVista)?.NodoSeleccionado?.ComandoAlHacerDobleClic;

        if (comando?.CanExecute(null) == true)
        {
            comando.Execute(null);
        }
    }
}
