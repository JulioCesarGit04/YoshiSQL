using Avalonia.Controls;
using Avalonia.Input;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Escritorio.ModelosDeVista.Resultados;

namespace YoshiSQL.Escritorio.Vistas.Resultados;

public partial class PanelDeResultados : UserControl
{
    public PanelDeResultados()
    {
        InitializeComponent();
    }

    // Doble clic en un error lleva a su línea en el editor, como en SSMS
    private void AlHacerDobleClicEnMensaje(object? remitente, TappedEventArgs argumentos)
    {
        if (DataContext is ResultadosModeloDeVista modelo && ListaDeMensajes.SelectedItem is MensajeDeEjecucion mensaje)
        {
            modelo.SolicitarIrALinea(mensaje);
        }
    }
}
