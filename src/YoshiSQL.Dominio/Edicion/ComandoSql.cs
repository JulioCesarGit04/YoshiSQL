namespace YoshiSQL.Dominio.Edicion;

/// <summary>
/// Instrucción con parámetros: los valores nunca se concatenan al texto, así que no hay riesgo de inyección SQL.
/// </summary>
/// <param name="FilasEsperadas">
/// Filas que debe afectar (1 en un UPDATE o DELETE por llave). Si afecta otra cantidad, alguien cambió
/// o borró la fila mientras tanto y toda la transacción se deshace.
/// </param>
public sealed record ComandoSql(string Texto, IReadOnlyList<ParametroSql> Parametros, int? FilasEsperadas = null);

/// <param name="TipoEnElServidor">
/// Tipo de la columna (ej. "datetime2"). Sin él, SqlClient manda toda fecha como datetime: redondea a 3 ms
/// las columnas datetime2 y la fila ya no coincide con la llave al guardar.
/// </param>
public sealed record ParametroSql(string Nombre, object? Valor, string? TipoEnElServidor = null);
