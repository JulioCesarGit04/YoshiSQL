using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace YoshiSQL.Escritorio.Convertidores;

/// <summary>
/// Devuelve true si la colección tiene elementos. Con el parámetro "vacio" invierte
/// el resultado (true cuando está vacía), para mostrar un mensaje de "no hay nada".
/// </summary>
public sealed class HayElementos : IValueConverter
{
    public static readonly HayElementos Instancia = new();

    public object Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura)
    {
        var tieneElementos = valor is IEnumerable elementos && elementos.Cast<object?>().Any();
        var invertir = string.Equals(parametro as string, "vacio", StringComparison.OrdinalIgnoreCase);
        return tieneElementos ^ invertir;
    }

    public object? ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();
}
