using Avalonia.Controls;
using Avalonia.Threading;
using YoshiSQL.Escritorio.ModelosDeVista.Editor;
using YoshiSQL.Escritorio.ModelosDeVista.Resultados;

namespace YoshiSQL.Escritorio.Vistas.Editor;

public partial class PestanaDeConsulta : UserControl
{
    private PestanaDeConsultaModeloDeVista? _pestanaObservada;
    private ResultadosModeloDeVista? _resultadosObservados;

    public PestanaDeConsulta()
    {
        InitializeComponent();
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
        }

        _pestanaObservada = DataContext as PestanaDeConsultaModeloDeVista;
        _resultadosObservados = _pestanaObservada?.Resultados;

        if (_pestanaObservada is not null)
        {
            _pestanaObservada.BusquedaSolicitada += AbrirBusqueda;
        }

        if (_resultadosObservados is not null)
        {
            _resultadosObservados.IrALineaSolicitado += IrALinea;
            Dispatcher.UIThread.Post(() => Editor.TextArea.Focus(), DispatcherPriority.Background);
        }
    }

    private void IrALinea(object? remitente, int numeroDeLinea) => Editor.IrALinea(numeroDeLinea);

    private void AbrirBusqueda(object? remitente, bool conReemplazo) => Editor.AbrirBusqueda(conReemplazo);
}
