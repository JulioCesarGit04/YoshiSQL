using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using YoshiSQL.Escritorio.ModelosDeVista;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.Vistas;

public partial class VentanaPrincipal : Window
{
    private const int ColumnaDelExplorador = 0;
    private const int ColumnaDelSeparador = 1;
    private const double AnchoDelSeparador = 5;

    private bool _cierreConfirmado;
    private DisposicionDeLaInterfaz? _disposicion;

    public VentanaPrincipal()
    {
        InitializeComponent();
        Opened += AlAbrirLaVentana;
    }

    private async void AlAbrirLaVentana(object? remitente, EventArgs argumentos)
    {
        if (DataContext is not VentanaPrincipalModeloDeVista modelo)
        {
            return;
        }

        _disposicion = modelo.Disposicion;
        await _disposicion.CargarAsync();
        AplicarDisposicionGuardada(_disposicion);
        _disposicion.PropertyChanged += AlCambiarLaDisposicion;

        await modelo.IniciarAsync();
    }

    /// <summary>
    /// La ventana abre con el tamaño y los paneles como quedaron la última vez.
    /// </summary>
    private void AplicarDisposicionGuardada(DisposicionDeLaInterfaz disposicion)
    {
        if (disposicion.EstaMaximizada)
        {
            WindowState = WindowState.Maximized;
        }
        else
        {
            // Si se guardó en un monitor más grande que el actual, no debe salirse de la pantalla
            var areaDeTrabajo = Screens.ScreenFromWindow(this)?.WorkingArea;
            var escala = RenderScaling;
            var anchoDisponible = areaDeTrabajo is { } area ? area.Width / escala : double.MaxValue;
            var altoDisponible = areaDeTrabajo is { } otraArea ? otraArea.Height / escala : double.MaxValue;

            Width = Math.Min(disposicion.Ancho, anchoDisponible);
            Height = Math.Min(disposicion.Alto, altoDisponible);
        }

        MostrarUOcultarElExplorador(disposicion);
    }

    private void AlCambiarLaDisposicion(object? remitente, PropertyChangedEventArgs argumentos)
    {
        if (argumentos.PropertyName == nameof(DisposicionDeLaInterfaz.ExploradorVisible) && _disposicion is not null)
        {
            MostrarUOcultarElExplorador(_disposicion);
        }
    }

    private void MostrarUOcultarElExplorador(DisposicionDeLaInterfaz disposicion)
    {
        var columnas = CuadriculaPrincipal.ColumnDefinitions;

        // Antes de ocultarlo se recuerda su ancho, para devolverlo igual
        if (!disposicion.ExploradorVisible && columnas[ColumnaDelExplorador].ActualWidth > 0)
        {
            disposicion.AnchoDelExplorador = columnas[ColumnaDelExplorador].ActualWidth;
        }

        columnas[ColumnaDelExplorador].Width = new GridLength(disposicion.ExploradorVisible ? disposicion.AnchoDelExplorador : 0);
        columnas[ColumnaDelSeparador].Width = new GridLength(disposicion.ExploradorVisible ? AnchoDelSeparador : 0);
        SeparadorDelExplorador.IsVisible = disposicion.ExploradorVisible;
    }

    private async Task GuardarDisposicionAsync()
    {
        if (_disposicion is null)
        {
            return;
        }

        try
        {
            _disposicion.EstaMaximizada = WindowState == WindowState.Maximized;

            // Maximizada o minimizada, el tamaño actual no es el que el usuario eligió: se conserva el anterior
            if (WindowState == WindowState.Normal)
            {
                _disposicion.Ancho = ClientSize.Width;
                _disposicion.Alto = ClientSize.Height;
            }

            if (_disposicion.ExploradorVisible)
            {
                _disposicion.AnchoDelExplorador = CuadriculaPrincipal.ColumnDefinitions[ColumnaDelExplorador].ActualWidth;
            }

            await _disposicion.GuardarAsync();
        }
        catch (Exception)
        {
            // Recordar el tamaño de la ventana es una comodidad: nunca debe impedir cerrar la aplicación
        }
    }

    // Clic con la rueda del mouse sobre una pestaña: la cierra, como en los navegadores
    private void AlPresionarPestana(object? remitente, PointerPressedEventArgs argumentos)
    {
        if (remitente is Control { DataContext: DocumentoModeloDeVista documento } control
            && argumentos.GetCurrentPoint(control).Properties.IsMiddleButtonPressed
            && DataContext is VentanaPrincipalModeloDeVista modelo)
        {
            argumentos.Handled = true;
            modelo.CerrarPestanaCommand.Execute(documento);
        }
    }

    private void AlCerrarEstaPestana(object? remitente, RoutedEventArgs argumentos)
    {
        if (ObtenerDocumentoDelMenu(remitente) is { } documento && DataContext is VentanaPrincipalModeloDeVista modelo)
        {
            modelo.CerrarPestanaCommand.Execute(documento);
        }
    }

    private void AlCerrarLasDemasPestanas(object? remitente, RoutedEventArgs argumentos)
    {
        if (ObtenerDocumentoDelMenu(remitente) is { } documento && DataContext is VentanaPrincipalModeloDeVista modelo)
        {
            modelo.CerrarOtrasPestanasCommand.Execute(documento);
        }
    }

    private void AlCerrarTodasLasPestanas(object? remitente, RoutedEventArgs argumentos)
    {
        if (DataContext is VentanaPrincipalModeloDeVista modelo)
        {
            modelo.CerrarTodasLasPestanasCommand.Execute(null);
        }
    }

    // El menú contextual hereda como contexto el documento de la pestaña sobre la que se abrió
    private static DocumentoModeloDeVista? ObtenerDocumentoDelMenu(object? remitente) =>
        (remitente as Control)?.DataContext as DocumentoModeloDeVista;

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
            await GuardarDisposicionAsync();
            _cierreConfirmado = true;
            Close();
        }
    }
}
