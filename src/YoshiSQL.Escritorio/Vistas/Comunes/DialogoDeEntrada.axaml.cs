using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Ventana para pedir un texto al usuario (por ejemplo, el nuevo nombre de un objeto).
/// Devuelve el texto escrito o null si se cancela.
/// </summary>
public partial class DialogoDeEntrada : Window
{
    public DialogoDeEntrada()
    {
        InitializeComponent();
    }

    public DialogoDeEntrada(string titulo, string etiqueta, string valorInicial)
        : this()
    {
        Title = titulo;
        Etiqueta.Text = etiqueta;
        CajaDeTexto.Text = valorInicial;
        Opened += (_, _) =>
        {
            CajaDeTexto.SelectAll();
            CajaDeTexto.Focus();
        };
    }

    private void AlAceptar(object? remitente, RoutedEventArgs argumentos) => Aceptar();

    private void AlCancelar(object? remitente, RoutedEventArgs argumentos) => Close(null);

    private void AlPresionarTecla(object? remitente, KeyEventArgs argumentos)
    {
        if (argumentos.Key == Key.Enter)
        {
            Aceptar();
        }
    }

    private void Aceptar()
    {
        var texto = CajaDeTexto.Text?.Trim();
        Close(string.IsNullOrEmpty(texto) ? null : texto);
    }
}
