using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using YoshiSQL.Aplicacion.Preferencias;
using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Escritorio.Servicios;

/// <summary>
/// Lleva las preferencias a la interfaz: tema de la aplicación y recursos del editor
/// (las vistas los leen con DynamicResource, así que el cambio se ve al instante).
/// </summary>
public sealed class AplicadorDePreferencias
{
    public const string ClaveDeLaFuenteDelEditor = "FuenteDelEditor";
    public const string ClaveDelTamanoDeLetraDelEditor = "TamanoDeLetraDelEditor";
    public const string ClaveDeMostrarNumerosDeLinea = "MostrarNumerosDeLinea";
    public const string ClaveDeAjustarLineasLargas = "AjustarLineasLargas";

    public AplicadorDePreferencias(ServicioDePreferencias servicioDePreferencias)
    {
        // Los recursos de la interfaz solo se pueden tocar desde su propio hilo
        servicioDePreferencias.PreferenciasCambiadas += (_, preferencias) => Dispatcher.UIThread.Post(() => Aplicar(preferencias));
    }

    public static void Aplicar(PreferenciasDelUsuario preferencias)
    {
        if (Application.Current is not { } aplicacion)
        {
            return;
        }

        aplicacion.RequestedThemeVariant = preferencias.Tema switch
        {
            TemaVisual.Claro => ThemeVariant.Light,
            TemaVisual.Sistema => ThemeVariant.Default,
            _ => ThemeVariant.Dark
        };

        aplicacion.Resources[ClaveDeLaFuenteDelEditor] = new FontFamily(preferencias.FuenteDelEditor);
        aplicacion.Resources[ClaveDelTamanoDeLetraDelEditor] = preferencias.TamanoDeLetraDelEditor;
        aplicacion.Resources[ClaveDeMostrarNumerosDeLinea] = preferencias.MostrarNumerosDeLinea;
        aplicacion.Resources[ClaveDeAjustarLineasLargas] = preferencias.AjustarLineasLargas;
    }
}
