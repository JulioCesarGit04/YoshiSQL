using Avalonia.Controls;
using Avalonia.Interactivity;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

public partial class DialogoAcercaDe : Window
{
    public DialogoAcercaDe()
    {
        InitializeComponent();
    }

    public DialogoAcercaDe(IServicioDelSistemaOperativo sistemaOperativo)
        : this()
    {
        TextoDeVersion.Text = InformacionDelEntorno.VersionDeYoshiSql;
        TextoDeDotNet.Text = InformacionDelEntorno.VersionDeDotNet;
        TextoDelSistema.Text = InformacionDelEntorno.SistemaOperativo;
        TextoDeConfiguracion.Text = sistemaOperativo.CarpetaDeConfiguracion;
        TextoDeRegistros.Text = sistemaOperativo.CarpetaDeRegistros;
    }

    private void AlAceptar(object? remitente, RoutedEventArgs argumentos) => Close();
}
