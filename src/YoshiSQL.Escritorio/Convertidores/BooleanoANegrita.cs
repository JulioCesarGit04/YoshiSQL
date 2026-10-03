using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace YoshiSQL.Escritorio.Convertidores;

public sealed class BooleanoANegrita : IValueConverter
{
    public static readonly BooleanoANegrita Instancia = new();

    public object? Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        valor is true ? FontWeight.Bold : FontWeight.Normal;

    public object? ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();
}
