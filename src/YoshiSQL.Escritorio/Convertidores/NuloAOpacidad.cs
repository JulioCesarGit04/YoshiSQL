using System.Globalization;
using Avalonia.Data.Converters;

namespace YoshiSQL.Escritorio.Convertidores;

/// <summary>Un valor nulo se dibuja atenuado.</summary>
public sealed class NuloAOpacidad : IValueConverter
{
    public static readonly NuloAOpacidad Instancia = new();

    private const double OpacidadDeUnNulo = 0.5;
    private const double OpacidadNormal = 1;

    public object Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        valor is null ? OpacidadDeUnNulo : OpacidadNormal;

    public object ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();
}
