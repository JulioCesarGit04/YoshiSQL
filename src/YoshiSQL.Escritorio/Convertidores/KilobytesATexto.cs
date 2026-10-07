using System.Globalization;
using Avalonia.Data.Converters;

namespace YoshiSQL.Escritorio.Convertidores;

/// <summary>
/// Convierte una cantidad de kilobytes en un texto legible: KB, MB o GB.
/// </summary>
public sealed class KilobytesATexto : IValueConverter
{
    public static readonly KilobytesATexto Instancia = new();

    public object? Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura)
    {
        if (valor is not long kilobytes)
        {
            return valor?.ToString();
        }

        return kilobytes switch
        {
            >= 1024 * 1024 => $"{(kilobytes / (1024.0 * 1024.0)).ToString("N2", cultura)} GB",
            >= 1024 => $"{(kilobytes / 1024.0).ToString("N2", cultura)} MB",
            _ => $"{kilobytes.ToString("N0", cultura)} KB"
        };
    }

    public object? ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();
}
