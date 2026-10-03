using Avalonia.Controls;
using Avalonia.Interactivity;

namespace YoshiSQL.Escritorio.Vistas.Administracion;

public partial class DialogoDeRestauracion : Window
{
    public DialogoDeRestauracion()
    {
        InitializeComponent();
    }

    private void AlCerrar(object? remitente, RoutedEventArgs argumentos) => Close();
}
