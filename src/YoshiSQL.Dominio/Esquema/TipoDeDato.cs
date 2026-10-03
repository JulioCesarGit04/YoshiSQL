namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Tipo de una columna con su longitud o precisión, ej. nvarchar(50) o decimal(18,2).
/// </summary>
public sealed record TipoDeDato(string Nombre, int? Longitud = null, int? Precision = null, int? Escala = null)
{
    // SQL Server representa las longitudes "max" con -1
    private const int LongitudMaxima = -1;

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

    /// <summary>
    /// Interpreta lo que escribe el usuario: "int", "nvarchar(100)", "varchar(max)" o "decimal(18,2)".
    /// </summary>
    /// <returns>El tipo, o nulo si el texto no tiene un formato válido.</returns>
    public static TipoDeDato? Interpretar(string texto)
    {
        var textoLimpio = texto.Trim().ToLowerInvariant();
        var inicioDeParentesis = textoLimpio.IndexOf('(');

        if (inicioDeParentesis < 0)
        {
            return EsNombreValido(textoLimpio) ? new TipoDeDato(textoLimpio) : null;
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

        return argumentos switch
        {
            ["max"] => new TipoDeDato(nombre, LongitudMaxima),
            [var longitud] when int.TryParse(longitud, out var valor) && valor > 0 => new TipoDeDato(nombre, valor),
            [var precision, var escala] when int.TryParse(precision, out var p) && int.TryParse(escala, out var e) && p > 0 && e >= 0 && e <= p
                => new TipoDeDato(nombre, Precision: p, Escala: e),
            _ => null
        };
    }

    private static bool EsNombreValido(string nombre) =>
        nombre.Length > 0 && nombre.All(caracter => char.IsLetterOrDigit(caracter) || caracter == '_');
}
