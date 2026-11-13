using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using YoshiSQL.Escritorio.ModelosDeVista;
using YoshiSQL.Escritorio.ModelosDeVista.Editor;
using YoshiSQL.Escritorio.ModelosDeVista.Resultados;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.Vistas.Editor;

public partial class PestanaDeConsulta : UserControl
{
    private const int FilaDelSeparador = 1;
    private const int FilaDeResultados = 2;
    private const double AltoDelSeparador = 5;

    // En una ventana baja, el panel de resultados (de alto fijo) dejaba el editor en dos líneas
    private const double ProporcionMaximaDeResultados = 0.6;
    private const double AltoMinimoDeResultados = 90;

    private PestanaDeConsultaModeloDeVista? _pestanaObservada;
    private ResultadosModeloDeVista? _resultadosObservados;
    private DisposicionDeLaInterfaz? _disposicion;

    public PestanaDeConsulta()
    {
        InitializeComponent();
        SeparadorDeResultados.DragCompleted += (_, _) => RecordarAltoDeResultados();

        // Solo es un tope: el alto que eligió el usuario se conserva y vuelve al agrandar la ventana
        Cuadricula.SizeChanged += (_, argumentos) =>
            Cuadricula.RowDefinitions[FilaDeResultados].MaxHeight =
                Math.Max(AltoMinimoDeResultados, argumentos.NewSize.Height * ProporcionMaximaDeResultados);
    }

    /// <summary>
    /// El alto del panel de resultados y si está visible son de toda la aplicación, no de cada pestaña:
    /// se leen de la ventana principal al mostrarse y se guardan al cerrar YoshiSQL.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs argumentos)
    {
        base.OnAttachedToVisualTree(argumentos);
        _disposicion = (TopLevel.GetTopLevel(this)?.DataContext as VentanaPrincipalModeloDeVista)?.Disposicion;

        if (_disposicion is not null)
        {
            _disposicion.PropertyChanged += AlCambiarLaDisposicion;
            AplicarDisposicionDeResultados();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs argumentos)
    {
        if (_disposicion is not null)
        {
            _disposicion.PropertyChanged -= AlCambiarLaDisposicion;
            _disposicion = null;
        }

        base.OnDetachedFromVisualTree(argumentos);
    }

    private void AlCambiarLaDisposicion(object? remitente, PropertyChangedEventArgs argumentos)
    {
        if (argumentos.PropertyName == nameof(DisposicionDeLaInterfaz.ResultadosVisibles))
        {
            AplicarDisposicionDeResultados();
        }
    }

    private void AplicarDisposicionDeResultados()
    {
        if (_disposicion is null)
        {
            return;
        }

        var visibles = _disposicion.ResultadosVisibles;
        var filas = Cuadricula.RowDefinitions;

        filas[FilaDeResultados].Height = new GridLength(visibles ? _disposicion.AltoDeResultados : 0);
        filas[FilaDelSeparador].Height = new GridLength(visibles ? AltoDelSeparador : 0);
        SeparadorDeResultados.IsVisible = visibles;
        PanelDeResultados.IsVisible = visibles;
    }

    private void RecordarAltoDeResultados()
    {
        if (_disposicion is { ResultadosVisibles: true })
        {
            _disposicion.AltoDeResultados = Cuadricula.RowDefinitions[FilaDeResultados].ActualHeight;
        }
    }

    // Al ejecutar con el panel oculto se vuelve a mostrar: si no, un error pasaría sin verse
    private void AlCambiarLaPestana(object? remitente, PropertyChangedEventArgs argumentos)
    {
        if (argumentos.PropertyName == nameof(PestanaDeConsultaModeloDeVista.EstaEjecutando)
            && _pestanaObservada is { EstaEjecutando: true }
            && _disposicion is not null)
        {
            _disposicion.ResultadosVisibles = true;
        }
    }

    /// <summary>
    /// La misma vista se reutiliza al cambiar de pestaña, así que al cambiar el modelo
    /// se mueve la suscripción de "ir a la línea del error" al nuevo modelo.
    /// </summary>
    protected override void OnDataContextChanged(EventArgs argumentos)
    {
        base.OnDataContextChanged(argumentos);

        if (_resultadosObservados is not null)
        {
            _resultadosObservados.IrALineaSolicitado -= IrALinea;
        }

        if (_pestanaObservada is not null)
        {
            _pestanaObservada.BusquedaSolicitada -= AbrirBusqueda;
            _pestanaObservada.ComentarioSolicitado -= AlComentario;
            _pestanaObservada.CambioDeCapitalizacionSolicitado -= ConvertirSeleccion;
            _pestanaObservada.PropertyChanged -= AlCambiarLaPestana;
        }

        _pestanaObservada = DataContext as PestanaDeConsultaModeloDeVista;
        _resultadosObservados = _pestanaObservada?.Resultados;

        if (_pestanaObservada is not null)
        {
            _pestanaObservada.BusquedaSolicitada += AbrirBusqueda;
            _pestanaObservada.ComentarioSolicitado += AlComentario;
            _pestanaObservada.CambioDeCapitalizacionSolicitado += ConvertirSeleccion;
            _pestanaObservada.PropertyChanged += AlCambiarLaPestana;
        }

        if (_resultadosObservados is not null)
        {
            _resultadosObservados.IrALineaSolicitado += IrALinea;
            Dispatcher.UIThread.Post(() => Editor.TextArea.Focus(), DispatcherPriority.Background);
        }
    }

    private void IrALinea(object? remitente, int numeroDeLinea) => Editor.IrALinea(numeroDeLinea);

    private void AbrirBusqueda(object? remitente, bool conReemplazo) => Editor.AbrirBusqueda(conReemplazo);

    private void AlComentario(object? remitente, AccionDeComentario accion)
    {
        switch (accion)
        {
            case AccionDeComentario.Comentar:
                Editor.Comentar();
                break;
            case AccionDeComentario.Descomentar:
                Editor.Descomentar();
                break;
            default:
                Editor.AlternarComentario();
                break;
        }
    }

    private void ConvertirSeleccion(object? remitente, bool aMayusculas) => Editor.ConvertirSeleccion(aMayusculas);
}
