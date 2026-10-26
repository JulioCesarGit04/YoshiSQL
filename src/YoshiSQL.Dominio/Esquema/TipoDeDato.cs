namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Tipo de una columna con su longitud o precisión, ej. nvarchar(50), decimal(18,2) o datetime2(3).
/// </summary>
/// <remarks>
/// Dos tipos son iguales aunque difieran en mayúsculas ("Name" y "name"): si no, el diseñador marcaba como
/// modificada cada columna de un tipo de usuario y generaba ALTER COLUMN innecesarios.
/// </remarks>
public sealed record TipoDeDato(string Nombre, int? Longitud = null, int? Precision = null, int? Escala = null)
{
    // SQL Server representa las longitudes "max" con -1
    private const int LongitudMaxima = -1;

    // Tipos del sistema: se escriben en minúsculas. Los tipos de usuario conservan sus mayúsculas,
    // porque en una base con intercalación sensible a mayúsculas "name" no es el tipo "Name".
    private static readonly HashSet<string> TiposDelSistema = new(StringComparer.OrdinalIgnoreCase)
    {
        "bigint", "binary", "bit", "char", "date", "datetime", "datetime2", "datetimeoffset", "decimal", "float",
        "geography", "geometry", "hierarchyid", "image", "int", "money", "nchar", "ntext", "numeric", "nvarchar",
        "real", "rowversion", "smalldatetime", "smallint", "smallmoney", "sql_variant", "sysname", "text", "time",
        "timestamp", "tinyint", "uniqueidentifier", "varbinary", "varchar", "xml"
    };

    // Tipos de fecha y hora cuya precisión de fracciones de segundo puede ser 0, ej. datetime2(0)
    private static readonly HashSet<string> TiposConPrecisionDeSegundos = new(StringComparer.OrdinalIgnoreCase)
    {
        "datetime2", "time", "datetimeoffset"
    };

    public string Describir()
    {
        if (Longitud is LongitudMaxima)
        {
            return $"{Nombre}(max)";
        }

        if (Longitud is not null)
        {
            return $"{Nombre}({Longitud})";
        }

        if (Precision is not null && Escala is not null)
        {
            return $"{Nombre}({Precision},{Escala})";
        }

        return Nombre;
    }

    public override string ToString() => Describir();

    public bool Equals(TipoDeDato? otro) =>
        otro is not null
        && string.Equals(Nombre, otro.Nombre, StringComparison.OrdinalIgnoreCase)
        && Longitud == otro.Longitud
        && Precision == otro.Precision
        && Escala == otro.Escala;

    public override int GetHashCode() =>
        HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Nombre), Longitud, Precision, Escala);

    /// <summary>
    /// Interpreta lo que escribe el usuario: "int", "nvarchar(100)", "varchar(max)", "decimal(18,2)" o "datetime2(3)".
    /// </summary>
    /// <returns>El tipo, o nulo si el texto no tiene un formato válido.</returns>
    public static TipoDeDato? Interpretar(string texto)
    {
        var textoLimpio = texto.Trim();
        var inicioDeParentesis = textoLimpio.IndexOf('(');

        if (inicioDeParentesis < 0)
        {
            return EsNombreValido(textoLimpio) ? new TipoDeDato(NormalizarNombre(textoLimpio)) : null;
        }

        if (!textoLimpio.EndsWith(')'))
        {
            return null;
        }

        var nombre = textoLimpio[..inicioDeParentesis].Trim();
        var argumentos = textoLimpio[(inicioDeParentesis + 1)..^1].Split(',', StringSplitOptions.TrimEntries);

        if (!EsNombreValido(nombre))
        {
            return null;
        }

        nombre = NormalizarNombre(nombre);
        var admiteCero = TiposConPrecisionDeSegundos.Contains(nombre);

        return argumentos switch
        {
            [var maximo] when maximo.Equals("max", StringComparison.OrdinalIgnoreCase) => new TipoDeDato(nombre, LongitudMaxima),
            [var longitud] when int.TryParse(longitud, out var valor) && (valor > 0 || (valor == 0 && admiteCero)) => new TipoDeDato(nombre, valor),
            [var precision, var escala] when int.TryParse(precision, out var p) && int.TryParse(escala, out var e) && p > 0 && e >= 0 && e <= p
                => new TipoDeDato(nombre, Precision: p, Escala: e),
            _ => null
        };
    }

    private static string NormalizarNombre(string nombre) =>
        TiposDelSistema.Contains(nombre) ? nombre.ToLowerInvariant() : nombre;

    private static bool EsNombreValido(string nombre) =>
        nombre.Length > 0 && nombre.All(caracter => char.IsLetterOrDigit(caracter) || caracter == '_');
}
