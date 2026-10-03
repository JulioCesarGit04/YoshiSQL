using Avalonia.Controls;
using YoshiSQL.Escritorio.ModelosDeVista.Preferencias;

namespace YoshiSQL.Escritorio.Vistas.Preferencias;

public partial class DialogoDePreferencias : Window
{
    public DialogoDePreferencias()
    {
        InitializeComponent();
    }

    public DialogoDePreferencias(DialogoDePreferenciasModeloDeVista modelo)
        : this()
    {
        DataContext = modelo;
        modelo.CierreSolicitado += (_, _) => Close();
    }
}
