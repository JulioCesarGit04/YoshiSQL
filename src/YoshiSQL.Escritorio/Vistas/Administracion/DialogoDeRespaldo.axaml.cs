using Avalonia.Controls;
using Avalonia.Interactivity;

namespace YoshiSQL.Escritorio.Vistas.Administracion;

public partial class DialogoDeRespaldo : Window
{
    public DialogoDeRespaldo()
    {
        InitializeComponent();
    }

    private void AlCerrar(object? remitente, RoutedEventArgs argumentos) => Close();
}
