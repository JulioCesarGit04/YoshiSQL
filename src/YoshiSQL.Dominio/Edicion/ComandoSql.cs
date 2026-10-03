namespace YoshiSQL.Dominio.Edicion;

/// <summary>
/// Instrucción con parámetros: los valores nunca se concatenan al texto, así que no hay riesgo de inyección SQL.
/// </summary>
/// <param name="FilasEsperadas">
/// Filas que debe afectar (1 en un UPDATE o DELETE por llave). Si afecta otra cantidad, alguien cambió
/// o borró la fila mientras tanto y toda la transacción se deshace.
/// </param>
public sealed record ComandoSql(string Texto, IReadOnlyList<ParametroSql> Parametros, int? FilasEsperadas = null);

public sealed record ParametroSql(string Nombre, object? Valor);
