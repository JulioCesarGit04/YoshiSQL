using Avalonia.Controls;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Escritorio.ModelosDeVista.Conexiones;

namespace YoshiSQL.Escritorio.Vistas.Conexiones;

public partial class DialogoDeConexion : Window
{
    public DialogoDeConexion()
    {
        InitializeComponent();
    }

    public DialogoDeConexion(DialogoDeConexionModeloDeVista modelo)
        : this()
    {
        DataContext = modelo;
        modelo.CierreSolicitado += CerrarConResultado;
        Closed += (_, _) => modelo.CierreSolicitado -= CerrarConResultado;
    }

    private void CerrarConResultado(object? remitente, ServidorConectado? servidor) => Close(servidor);
}
