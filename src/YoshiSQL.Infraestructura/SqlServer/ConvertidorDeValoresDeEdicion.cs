using System.Globalization;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Convierte lo que el usuario escribe en una celda al tipo real de la columna, para enviarlo
/// como parámetro tipado. Así "12.5" llega como número y "2026-01-05" como fecha.
/// </summary>
internal static class ConvertidorDeValoresDeEdicion
{
    private static readonly HashSet<string> TiposEnteros = new(["int", "bigint", "smallint", "tinyint"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> TiposDecimales = new(["decimal", "numeric", "money", "smallmoney"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> TiposDeComaFlotante = new(["float", "real"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> TiposDeFecha = new(["date", "datetime", "datetime2", "smalldatetime"], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> TiposBinarios = new(["binary", "varbinary", "image"], StringComparer.OrdinalIgnoreCase);

    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    /// <param name="valor">Texto escrito por el usuario, el valor original sin cambios, o nulo para NULL.</param>
    public static object? Convertir(object? valor, Columna columna)
    {
        if (valor is not string texto)
        {
            return valor;
        }

        var nombreDelTipo = columna.TipoDeDato.Nombre;

        try
        {
            return nombreDelTipo.ToLowerInvariant() switch
            {
                var tipo when TiposEnteros.Contains(tipo) => long.Parse(texto, NumberStyles.Integer, Invariante),
                var tipo when TiposDecimales.Contains(tipo) => decimal.Parse(texto, NumberStyles.Number, Invariante),
                var tipo when TiposDeComaFlotante.Contains(tipo) => double.Parse(texto, NumberStyles.Float, Invariante),
                var tipo when TiposDeFecha.Contains(tipo) => DateTime.Parse(texto, Invariante),
                var tipo when TiposBinarios.Contains(tipo) => ConvertirBinario(texto),
                "bit" => ConvertirBooleano(texto),
                "datetimeoffset" => DateTimeOffset.Parse(texto, Invariante),
                "time" => TimeSpan.Parse(texto, Invariante),
                "uniqueidentifier" => Guid.Parse(texto),
                _ => texto
            };
        }
        catch (Exception error) when (error is FormatException or OverflowException)
        {
            throw new ErrorDeYoshiSql($"El valor \"{texto}\" no es válido para la columna {columna.Nombre} ({columna.TipoDeDato.Describir()}).", error);
        }
    }

    private static bool ConvertirBooleano(string texto) => texto.Trim().ToLowerInvariant() switch
    {
        "1" or "true" or "verdadero" or "si" or "sí" => true,
        "0" or "false" or "falso" or "no" => false,
        _ => throw new FormatException()
    };

    private static byte[] ConvertirBinario(string texto)
    {
        var hexadecimal = texto.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? texto[2..] : texto;
        return Convert.FromHexString(hexadecimal);
    }
}
