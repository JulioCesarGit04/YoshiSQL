using Avalonia.Controls;
using YoshiSQL.Escritorio.ModelosDeVista;

namespace YoshiSQL.Escritorio.Vistas;

public partial class VentanaPrincipal : Window
{
    private bool _cierreConfirmado;

    public VentanaPrincipal()
    {
        InitializeComponent();
        Opened += AlAbrirLaVentana;
    }

    private async void AlAbrirLaVentana(object? remitente, EventArgs argumentos)
    {
        if (DataContext is VentanaPrincipalModeloDeVista modelo)
        {
            await modelo.IniciarAsync();
        }
    }

    /// <summary>
    /// Antes de cerrar pregunta por los scripts sin guardar. Como la pregunta es asíncrona,
    /// se cancela el cierre, se pregunta y luego se vuelve a cerrar si el usuario lo confirma.
    /// </summary>
    protected override async void OnClosing(WindowClosingEventArgs argumentos)
    {
        if (_cierreConfirmado || DataContext is not VentanaPrincipalModeloDeVista modelo)
        {
            base.OnClosing(argumentos);
            return;
        }

        argumentos.Cancel = true;

        if (await modelo.PrepararCierreAsync())
        {
            _cierreConfirmado = true;
            Close();
        }
    }
}
