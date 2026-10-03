using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Escritorio.Vistas.Comunes;

namespace YoshiSQL.Escritorio.Servicios;

public sealed class ServicioDeErrores : IServicioDeErrores
{
    private readonly RegistroDeErrores _registroDeErrores;
    private readonly IServicioDelSistemaOperativo _sistemaOperativo;
    private readonly ILogger<ServicioDeErrores> _registro;

    public ServicioDeErrores(
        RegistroDeErrores registroDeErrores,
        IServicioDelSistemaOperativo sistemaOperativo,
        ILogger<ServicioDeErrores> registro)
    {
        _registroDeErrores = registroDeErrores;
        _sistemaOperativo = sistemaOperativo;
        _registro = registro;
    }

    public string RegistrarYDescribir(Exception error, ContextoDeError contexto) =>
        _registroDeErrores.Registrar(error, contexto).MensajeParaElUsuario;

    public async Task RegistrarYMostrarAsync(Exception error, ContextoDeError contexto)
    {
        var errorRegistrado = _registroDeErrores.Registrar(error, contexto);

        if (error is OperationCanceledException)
        {
            return;
        }

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() => MostrarAvisoAsync(errorRegistrado));
        }
        // Si ni siquiera se puede mostrar el aviso, el error ya quedó registrado y no se insiste
        catch (Exception errorAlMostrar)
        {
            _registro.LogError(errorAlMostrar, "No se pudo mostrar el aviso del error {CodigoDeError}", errorRegistrado.Codigo);
        }
    }

    private async Task MostrarAvisoAsync(ErrorRegistrado errorRegistrado)
    {
        var dialogo = new DialogoDeError(errorRegistrado, _sistemaOperativo);
        var ventanaPrincipal = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (ventanaPrincipal is { IsVisible: true })
        {
            await dialogo.ShowDialog(ventanaPrincipal);
        }
        else
        {
            dialogo.Show();
        }
    }
}
