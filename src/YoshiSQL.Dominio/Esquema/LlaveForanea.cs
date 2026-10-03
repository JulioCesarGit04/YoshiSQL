namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Relación donde las columnas de la tabla origen apuntan a las de la tabla destino.
/// </summary>
public sealed record LlaveForanea(
    string Nombre,
    Tabla TablaOrigen,
    IReadOnlyList<string> ColumnasOrigen,
    Tabla TablaDestino,
    IReadOnlyList<string> ColumnasDestino);
