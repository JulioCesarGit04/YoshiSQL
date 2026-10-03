using System.Globalization;

namespace YoshiSQL.Infraestructura.Exportacion;

/// <summary>
/// Convierte los valores de una celda a texto de forma independiente del idioma del sistema,
/// para que un archivo exportado se lea igual en cualquier computadora.
/// </summary>
internal static class ConvertidorDeValoresATexto
{
    public static string? Convertir(object? valor) => valor switch
    {
        null => null,
        bool booleano => booleano ? "1" : "0",
        byte[] bytes => $"0x{Convert.ToHexString(bytes)}",
        DateTime fecha => fecha.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        DateTimeOffset fecha => fecha.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture),
        TimeSpan hora => hora.ToString("c", CultureInfo.InvariantCulture),
        IFormattable formateable => formateable.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString()
    };

    public static bool EsNumero(object? valor) =>
        valor is byte or short or int or long or decimal or double or float;
}
