using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using YoshiSQL.Dominio.Diagramas;
using YoshiSQL.Escritorio.ModelosDeVista.Diagramas;

namespace YoshiSQL.Escritorio.Vistas.Diagramas;

/// <summary>
/// Maneja lo que es puramente visual del diagrama: arrastrar tablas, aplicar el zoom, desplazarse hasta una tabla
/// y convertir lo dibujado en una imagen para guardarlo. Las posiciones, la selección y el zoom viven en el modelo de vista.
/// </summary>
public partial class PestanaDeDiagrama : UserControl
{
    private const string RecursoDelFondo = "PincelDeFondo";

    // La imagen se guarda al doble de resolución que la pantalla para que el texto se lea bien al ampliar o imprimir
    private const double EscalaAlGuardar = 2;
    private const double PuntosPorPulgadaDeLaPantalla = 96;
    private const double PuntosPorPulgadaDelPdf = 72;
    private const double MargenAlGuardar = 40;

    // Por encima de esto la imagen ocuparía cientos de MB en memoria: los diagramas enormes se guardan a menor resolución
    private const double LadoMaximoEnPixeles = 12000;

    private const int BytesPorPixelDeLaPantalla = 4;
    private const double MargenAlAjustar = 24;

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
            _modeloObservado.TablaEnfocada -= AlEnfocarUnaTabla;
        }

        _modeloObservado = Modelo;

        if (_modeloObservado is not null)
        {
            _modeloObservado.PropertyChanged += AlCambiarElModelo;
            _modeloObservado.TablaEnfocada += AlEnfocarUnaTabla;
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

    /// <summary>
    /// Deja la tabla encontrada en el centro de lo visible.
    /// </summary>
    private void AlEnfocarUnaTabla(object? remitente, TablaDelDiagramaModeloDeVista tabla)
    {
        if (Modelo is not { } modelo)
        {
            return;
        }

        var centroX = (tabla.X + (TablaDelDiagramaModeloDeVista.Ancho / 2)) * modelo.Zoom;
        var centroY = (tabla.Y + (tabla.Alto / 2)) * modelo.Zoom;

        Desplazador.Offset = new Vector(
            Math.Max(0, centroX - (Desplazador.Viewport.Width / 2)),
            Math.Max(0, centroY - (Desplazador.Viewport.Height / 2)));
    }

    private void AlPresionarTeclaEnLaBusqueda(object? remitente, KeyEventArgs argumentos)
    {
        if (argumentos.Key == Key.Enter && Modelo is { } modelo)
        {
            modelo.BuscarSiguienteTablaCommand.Execute(null);
            argumentos.Handled = true;
        }
    }

    /// <summary>
    /// Elige el zoom con el que todo el diagrama cabe en la ventana.
    /// </summary>
    private void AlAjustarALaVentana(object? remitente, RoutedEventArgs argumentos)
    {
        if (Modelo is not { AnchoDelContenido: > 0, AltoDelContenido: > 0 } modelo)
        {
            return;
        }

        var zoomHorizontal = (Desplazador.Viewport.Width - MargenAlAjustar) / modelo.AnchoDelContenido;
        var zoomVertical = (Desplazador.Viewport.Height - MargenAlAjustar) / modelo.AltoDelContenido;

        modelo.EstablecerZoom(Math.Min(zoomHorizontal, zoomVertical));
        Desplazador.Offset = default;
    }

    private void AlPresionarRelacion(object? remitente, PointerPressedEventArgs argumentos)
    {
        if (remitente is Control { DataContext: RelacionDelDiagramaModeloDeVista relacion } && Modelo is { } modelo)
        {
            modelo.SeleccionarRelacion(relacion);
            argumentos.Handled = true;
        }
    }

    // Tocar cualquier parte de una tabla muestra todas sus relaciones
    private void AlPresionarTabla(object? remitente, PointerPressedEventArgs argumentos)
    {
        if (remitente is Control { DataContext: TablaDelDiagramaModeloDeVista tabla } && Modelo is { } modelo)
        {
            modelo.SeleccionarTabla(tabla);
            argumentos.Handled = true;
        }
    }

    private void AlPresionarElFondo(object? remitente, PointerPressedEventArgs argumentos) =>
        Modelo?.QuitarSeleccionCommand.Execute(null);

    private void AlPresionarEncabezado(object? remitente, PointerPressedEventArgs argumentos)
    {
        if (remitente is not Control { DataContext: TablaDelDiagramaModeloDeVista tabla } encabezado
            || !argumentos.GetCurrentPoint(encabezado).Properties.IsLeftButtonPressed)
        {
            return;
        }

        Modelo?.TraerAlFrente(tabla);
        Modelo?.SeleccionarTabla(tabla);

        var posicionDelPuntero = argumentos.GetPosition(Lienzo);
        _distanciaDelPunteroAlBorde = new Point(posicionDelPuntero.X - tabla.X, posicionDelPuntero.Y - tabla.Y);
        _tablaArrastrada = tabla;
        argumentos.Pointer.Capture(encabezado);

        // Si el sistema le quita el puntero (cambio de ventana, un menú, Alt+Tab) nunca llega "soltar"
        // y la tabla quedaba pegada al mouse
        encabezado.PointerCaptureLost -= AlPerderElPuntero;
        encabezado.PointerCaptureLost += AlPerderElPuntero;
        argumentos.Handled = true;
    }

    private async void AlPerderElPuntero(object? remitente, PointerCaptureLostEventArgs argumentos)
    {
        if (_tablaArrastrada is null || Modelo is null)
        {
            return;
        }

        _tablaArrastrada = null;
        await Modelo.TerminarArrastreAsync();
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

    private async void AlGuardarComoImagen(object? remitente, RoutedEventArgs argumentos)
    {
        if (Modelo is not { Tablas.Count: > 0 } modelo)
        {
            return;
        }

        try
        {
            using var imagen = DibujarDiagrama(modelo, out _);
            using var png = new MemoryStream();
            imagen.Save(png, new PngBitmapEncoderOptions());
            await modelo.GuardarComoImagenAsync(png.ToArray());
        }
        catch (Exception error)
        {
            modelo.InformarErrorAlGuardar(error);
        }
    }

    private async void AlGuardarComoPdf(object? remitente, RoutedEventArgs argumentos)
    {
        if (Modelo is not { Tablas.Count: > 0 } modelo)
        {
            return;
        }

        try
        {
            using var imagen = DibujarDiagrama(modelo, out var escala);

            // La página del PDF mide lo mismo que el diagrama en pantalla (96 puntos de pantalla = 72 de imprenta)
            var puntosDelPdfPorPixel = PuntosPorPulgadaDelPdf / (PuntosPorPulgadaDeLaPantalla * escala);

            await modelo.GuardarComoPdfAsync(
                ConvertirARgb(imagen),
                imagen.PixelSize.Width * puntosDelPdfPorPixel,
                imagen.PixelSize.Height * puntosDelPdfPorPixel);
        }
        catch (Exception error)
        {
            modelo.InformarErrorAlGuardar(error);
        }
    }

    /// <summary>
    /// Dibuja el diagrama completo (también lo que queda fuera de la pantalla) tal como está acomodado,
    /// sin importar el zoom con que se esté viendo.
    /// </summary>
    /// <param name="escala">Píxeles de la imagen por cada punto de la pantalla.</param>
    private RenderTargetBitmap DibujarDiagrama(PestanaDeDiagramaModeloDeVista modelo, out double escala)
    {
        var ancho = modelo.AnchoDelContenido + MargenAlGuardar;
        var alto = modelo.AltoDelContenido + MargenAlGuardar;
        escala = Math.Min(EscalaAlGuardar, LadoMaximoEnPixeles / Math.Max(ancho, alto));

        var tamano = new PixelSize((int)Math.Ceiling(ancho * escala), (int)Math.Ceiling(alto * escala));
        var imagen = new RenderTargetBitmap(tamano, new Vector(PuntosPorPulgadaDeLaPantalla * escala, PuntosPorPulgadaDeLaPantalla * escala));

        // El lienzo es transparente (el fondo lo pone el panel de atrás): para la imagen se le da el color del tema
        var fondoAnterior = Lienzo.Background;
        Lienzo.Background = this.TryFindResource(RecursoDelFondo, ActualThemeVariant, out var recurso) && recurso is IBrush pincel
            ? pincel
            : Brushes.White;

        try
        {
            imagen.Render(Lienzo);
        }
        finally
        {
            Lienzo.Background = fondoAnterior;
        }

        return imagen;
    }

    /// <summary>
    /// Pasa la imagen al formato que necesita el PDF: tres bytes por punto, sin transparencia.
    /// </summary>
    private static ImagenRgb ConvertirARgb(RenderTargetBitmap imagen)
    {
        var ancho = imagen.PixelSize.Width;
        var alto = imagen.PixelSize.Height;
        var bytesPorFila = ancho * BytesPorPixelDeLaPantalla;
        var original = new byte[bytesPorFila * alto];

        var asa = GCHandle.Alloc(original, GCHandleType.Pinned);

        try
        {
            imagen.CopyPixels(new PixelRect(0, 0, ancho, alto), asa.AddrOfPinnedObject(), original.Length, bytesPorFila);
        }
        finally
        {
            asa.Free();
        }

        // Según el sistema, los colores vienen en orden azul-verde-rojo o rojo-verde-azul
        var rojoPrimero = imagen.Format == PixelFormats.Rgba8888;
        var rgb = new byte[ancho * alto * ImagenRgb.BytesPorPixel];

        for (int origen = 0, destino = 0; origen < original.Length; origen += BytesPorPixelDeLaPantalla, destino += ImagenRgb.BytesPorPixel)
        {
            rgb[destino] = original[rojoPrimero ? origen : origen + 2];
            rgb[destino + 1] = original[origen + 1];
            rgb[destino + 2] = original[rojoPrimero ? origen + 2 : origen];
        }

        return new ImagenRgb(ancho, alto, rgb);
    }
}
