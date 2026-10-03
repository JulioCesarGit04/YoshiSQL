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
}
