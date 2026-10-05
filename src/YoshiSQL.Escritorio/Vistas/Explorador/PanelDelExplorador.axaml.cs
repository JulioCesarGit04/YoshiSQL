using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using YoshiSQL.Escritorio.ModelosDeVista.Explorador;

namespace YoshiSQL.Escritorio.Vistas.Explorador;

public partial class PanelDelExplorador : UserControl
{
    public PanelDelExplorador()
    {
        InitializeComponent();

        // En fase de túnel porque el menú se abre antes de llegar a los manejadores del propio nodo,
        // y en el evento Opening el menú todavía no tiene el nodo como DataContext (siempre aparece vacío)
        ArbolDeObjetos.AddHandler(ContextRequestedEvent, AlPedirMenuContextual, RoutingStrategies.Tunnel);
    }

    // Los nodos sin acciones (columnas, índices) no deben mostrar un menú vacío
    private void AlPedirMenuContextual(object? remitente, ContextRequestedEventArgs argumentos)
    {
        if ((argumentos.Source as Control)?.DataContext is NodoDelArbolModeloDeVista { Acciones.Count: 0 })
        {
            argumentos.Handled = true;
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
