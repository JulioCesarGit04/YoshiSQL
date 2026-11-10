namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Fórmula de una columna calculada, ej. Ganancia AS ([Total]-[Costo]) PERSISTED.
/// </summary>
public sealed record ColumnaCalculada(Tabla Tabla, string Columna, string Definicion, bool EsPersistida);
