using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace YoshiSQL.Escritorio.Convertidores;

/// <summary>
/// Convierte una cadena "#RRGGBB" en un pincel; devuelve null si no hay color o no es válido.
/// </summary>
public sealed class CadenaHexAPincel : IValueConverter
{
    public static readonly CadenaHexAPincel Instancia = new();

    public object? Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        valor is string hex && Color.TryParse(hex, out var color) ? new SolidColorBrush(color) : null;

    public object? ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();
}
