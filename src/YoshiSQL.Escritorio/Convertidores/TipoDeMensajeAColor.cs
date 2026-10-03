using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.Convertidores;

/// <summary>
/// Errores en rojo y advertencias en amarillo, según el tema actual. Los mensajes informativos
/// no reciben color propio y usan el color de texto normal del tema.
/// </summary>
public sealed class TipoDeMensajeAColor : IValueConverter
{
    public static readonly TipoDeMensajeAColor Instancia = new();

    public object? Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        valor switch
        {
            TipoDeMensaje.Error => BuscarPincelDelTema("PincelDeError"),
            TipoDeMensaje.Advertencia => BuscarPincelDelTema("PincelDeAdvertencia"),
            _ => AvaloniaProperty.UnsetValue
        };

    public object? ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();

    private static object? BuscarPincelDelTema(string clave)
    {
        var aplicacion = Application.Current;
        return aplicacion is not null && aplicacion.TryGetResource(clave, aplicacion.ActualThemeVariant, out var pincel)
            ? pincel
            : AvaloniaProperty.UnsetValue;
    }
}
