namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Valor que toma una columna cuando el INSERT no la menciona (restricción DEFAULT).
/// </summary>
/// <param name="Nombre">Nulo si SQL Server le puso el nombre (DF__Tabla__Col__1A2B3C): al recrearla se deja que genere otro.</param>
/// <param name="Definicion">Expresión tal como la guarda el catálogo, ej. "((0))" o "(sysdatetime())".</param>
public sealed record ValorPredeterminado(Tabla Tabla, string Columna, string? Nombre, string Definicion);
