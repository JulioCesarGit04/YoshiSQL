using Avalonia.Controls;
using Avalonia.Interactivity;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Aviso amigable de un error: mensaje claro, código para buscarlo en el registro
/// y accesos para abrir la carpeta de registros o copiar el detalle técnico.
/// </summary>
public partial class DialogoDeError : Window
{
    private readonly ErrorRegistrado? _error;
    private readonly IServicioDelSistemaOperativo? _sistemaOperativo;

    public DialogoDeError()
    {
        InitializeComponent();
    }

    public DialogoDeError(ErrorRegistrado error, IServicioDelSistemaOperativo sistemaOperativo)
        : this()
    {
        _error = error;
        _sistemaOperativo = sistemaOperativo;

        Title = error.EsInesperado ? "Algo salió mal" : "No se pudo completar la acción";
        TextoDelMensaje.Text = error.MensajeParaElUsuario;
        TextoDelCodigo.Text = error.Codigo;
        MarcoDelCodigo.IsVisible = error.Codigo is not null;
        BotonAbrirRegistros.IsVisible = error.EsInesperado;
        BotonCopiar.IsVisible = error.EsInesperado;
    }

    private void AlAbrirRegistros(object? remitente, RoutedEventArgs argumentos)
    {
        try
        {
            _sistemaOperativo?.AbrirCarpetaDeRegistros();
        }
        catch (Exception error)
        {
            MostrarConfirmacion(error.Message);
        }
    }

    private async void AlCopiarDetalle(object? remitente, RoutedEventArgs argumentos)
    {
        if (_error is null || _sistemaOperativo is null)
        {
            return;
        }

        try
        {
            await _sistemaOperativo.CopiarAlPortapapelesAsync($"{_error.Codigo}{Environment.NewLine}{_error.DetalleTecnico}");
            MostrarConfirmacion("Detalle copiado al portapapeles.");
        }
        catch (Exception)
        {
            MostrarConfirmacion("No se pudo copiar; el detalle está en la carpeta de registros.");
        }
    }

    private void AlAceptar(object? remitente, RoutedEventArgs argumentos) => Close();

    private void MostrarConfirmacion(string texto)
    {
        TextoDeConfirmacion.Text = texto;
        TextoDeConfirmacion.IsVisible = true;
    }
}
