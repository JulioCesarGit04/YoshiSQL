using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using YoshiSQL.Escritorio.ModelosDeVista.Diagramas;

namespace YoshiSQL.Escritorio.Vistas.Diagramas;

/// <summary>
/// Maneja lo que es puramente visual del diagrama: arrastrar tablas y aplicar el zoom.
/// Las posiciones y el nivel de zoom viven en el modelo de vista.
/// </summary>
public partial class PestanaDeDiagrama : UserControl
{
    private PestanaDeDiagramaModeloDeVista? _modeloObservado;
    private TablaDelDiagramaModeloDeVista? _tablaArrastrada;
    private Point _distanciaDelPunteroAlBorde;

    public PestanaDeDiagrama()
    {
        InitializeComponent();

        // Se escucha en la fase de túnel para recibir Ctrl + rueda antes de que el ScrollViewer desplace
        Desplazador.AddHandler(PointerWheelChangedEvent, AlGirarLaRueda, RoutingStrategies.Tunnel);
    }

    private PestanaDeDiagramaModeloDeVista? Modelo => DataContext as PestanaDeDiagramaModeloDeVista;

    protected override void OnDataContextChanged(EventArgs argumentos)
    {
        base.OnDataContextChanged(argumentos);

        if (_modeloObservado is not null)
        {
            _modeloObservado.PropertyChanged -= AlCambiarElModelo;
        }

        _modeloObservado = Modelo;

        if (_modeloObservado is not null)
        {
            _modeloObservado.PropertyChanged += AlCambiarElModelo;
            AplicarZoom(_modeloObservado.Zoom);
        }
    }

    private void AlCambiarElModelo(object? remitente, PropertyChangedEventArgs argumentos)
    {
        if (argumentos.PropertyName == nameof(PestanaDeDiagramaModeloDeVista.Zoom) && _modeloObservado is not null)
        {
            AplicarZoom(_modeloObservado.Zoom);
        }
    }

    private void AplicarZoom(double zoom) => Transformador.LayoutTransform = new ScaleTransform(zoom, zoom);

    private void AlGirarLaRueda(object? remitente, PointerWheelEventArgs argumentos)
    {
        if (Modelo is null || !argumentos.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        Modelo.CambiarZoomConRueda(argumentos.Delta.Y);
        argumentos.Handled = true;
    }

    private void AlPresionarEncabezado(object? remitente, PointerPressedEventArgs argumentos)
    {
        if (remitente is not Control { DataContext: TablaDelDiagramaModeloDeVista tabla } encabezado
            || !argumentos.GetCurrentPoint(encabezado).Properties.IsLeftButtonPressed)
        {
            return;
        }

        Modelo?.TraerAlFrente(tabla);

        var posicionDelPuntero = argumentos.GetPosition(Lienzo);
        _distanciaDelPunteroAlBorde = new Point(posicionDelPuntero.X - tabla.X, posicionDelPuntero.Y - tabla.Y);
        _tablaArrastrada = tabla;
        argumentos.Pointer.Capture(encabezado);
        argumentos.Handled = true;
    }

    private void AlMoverPuntero(object? remitente, PointerEventArgs argumentos)
    {
        if (_tablaArrastrada is null || Modelo is null)
        {
            return;
        }

        var posicionDelPuntero = argumentos.GetPosition(Lienzo);
        Modelo.MoverTabla(
            _tablaArrastrada,
            posicionDelPuntero.X - _distanciaDelPunteroAlBorde.X,
            posicionDelPuntero.Y - _distanciaDelPunteroAlBorde.Y);
    }

    private async void AlSoltarPuntero(object? remitente, PointerReleasedEventArgs argumentos)
    {
        if (_tablaArrastrada is null || Modelo is null)
        {
            return;
        }

        _tablaArrastrada = null;
        argumentos.Pointer.Capture(null);
        await Modelo.TerminarArrastreAsync();
    }
}
