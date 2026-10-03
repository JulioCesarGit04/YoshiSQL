using Avalonia.Controls;
using Avalonia.Threading;
using YoshiSQL.Escritorio.ModelosDeVista.Editor;
using YoshiSQL.Escritorio.ModelosDeVista.Resultados;

namespace YoshiSQL.Escritorio.Vistas.Editor;

public partial class PestanaDeConsulta : UserControl
{
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

        _resultadosObservados = (DataContext as PestanaDeConsultaModeloDeVista)?.Resultados;

        if (_resultadosObservados is not null)
        {
            _resultadosObservados.IrALineaSolicitado += IrALinea;
            Dispatcher.UIThread.Post(() => Editor.TextArea.Focus(), DispatcherPriority.Background);
        }
    }

    private void IrALinea(object? remitente, int numeroDeLinea) => Editor.IrALinea(numeroDeLinea);
}
