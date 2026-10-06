using Avalonia.Controls;
using Avalonia.Interactivity;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Ventana para ver un texto largo (el valor de una celda, XML o JSON) con desplazamiento y copia.
/// </summary>
public partial class DialogoDeTexto : Window
{
    public DialogoDeTexto()
    {
        InitializeComponent();
    }

    public DialogoDeTexto(string titulo, string texto)
        : this()
    {
        Title = titulo;
        CajaDeTexto.Text = texto;
    }

    private void AlCerrar(object? remitente, RoutedEventArgs argumentos) => Close();
}
