using System.Globalization;
using Avalonia.Data.Converters;

namespace YoshiSQL.Escritorio.Convertidores;

/// <summary>
/// Muestra cada valor de la grilla como lo haría SSMS: NULL, bits como 1/0, binarios en hexadecimal.
/// </summary>
public sealed class ValorDeCeldaATexto : IValueConverter
{
    public static readonly ValorDeCeldaATexto Instancia = new();

    private const string TextoDeNulo = "NULL";
    private const int BytesBinariosVisibles = 64;

    public object? Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        valor switch
        {
            null => TextoDeNulo,
            bool booleano => booleano ? "1" : "0",
            byte[] bytes => FormatearBinario(bytes),
            DateTime fecha => fecha.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
            DateTimeOffset fecha => fecha.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
            IFormattable formateable => formateable.ToString(null, CultureInfo.InvariantCulture),
            _ => valor.ToString()
        };

    public object? ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();

    private static string FormatearBinario(byte[] bytes)
    {
        var bytesVisibles = bytes.AsSpan(0, Math.Min(bytes.Length, BytesBinariosVisibles));
        var sufijo = bytes.Length > BytesBinariosVisibles ? "..." : string.Empty;
        return $"0x{System.Convert.ToHexString(bytesVisibles)}{sufijo}";
    }
}
