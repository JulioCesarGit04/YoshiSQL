using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace YoshiSQL.Escritorio.Convertidores;

/// <summary>Un valor nulo se escribe en cursiva.</summary>
public sealed class NuloACursiva : IValueConverter
{
    public static readonly NuloACursiva Instancia = new();

    public object Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        valor is null ? FontStyle.Italic : FontStyle.Normal;

    public object ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();
}
