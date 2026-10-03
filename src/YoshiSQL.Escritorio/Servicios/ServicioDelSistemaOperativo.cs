using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Microsoft.Extensions.Logging;
using YoshiSQL.Infraestructura.Persistencia;

namespace YoshiSQL.Escritorio.Servicios;

public sealed class ServicioDelSistemaOperativo : IServicioDelSistemaOperativo
{
    // Programa estándar de Linux que abre carpetas y archivos con la aplicación predeterminada
    private const string ProgramaParaAbrir = "xdg-open";

    private readonly RutasDeLaAplicacion _rutas;
    private readonly ILogger<ServicioDelSistemaOperativo> _registro;

    public ServicioDelSistemaOperativo(RutasDeLaAplicacion rutas, ILogger<ServicioDelSistemaOperativo> registro)
    {
        _rutas = rutas;
        _registro = registro;
    }

    public string CarpetaDeRegistros => _rutas.CarpetaDeRegistros;

    public string CarpetaDeConfiguracion => _rutas.CarpetaDeConfiguracion;

    public void AbrirCarpetaDeRegistros()
    {
        Directory.CreateDirectory(CarpetaDeRegistros);

        try
        {
            Process.Start(new ProcessStartInfo(ProgramaParaAbrir, CarpetaDeRegistros) { UseShellExecute = false });
        }
        catch (Exception error)
        {
            _registro.LogWarning(error, "No se pudo abrir la carpeta de registros {Carpeta}", CarpetaDeRegistros);
            throw new Dominio.Errores.ErrorDeYoshiSql(
                $"No se pudo abrir la carpeta automáticamente. Los registros están en:\n{CarpetaDeRegistros}", error);
        }
    }

    public async Task CopiarAlPortapapelesAsync(string texto)
    {
        var ventanaPrincipal = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (ventanaPrincipal?.Clipboard is { } portapapeles)
        {
            await portapapeles.SetTextAsync(texto);
        }
    }
}
