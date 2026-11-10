namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Valor inicial e incremento de una columna IDENTITY, ej. IDENTITY(1000,5).
/// Van como texto porque la columna puede ser bigint o decimal(38,0).
/// </summary>
public sealed record IdentidadDeColumna(Tabla Tabla, string Columna, string Semilla, string Incremento);
