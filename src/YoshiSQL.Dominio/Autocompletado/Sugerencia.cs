namespace YoshiSQL.Dominio.Autocompletado;

/// <param name="Texto">Lo que se inserta en el editor.</param>
/// <param name="Detalle">Información adicional, ej. el tipo de dato de una columna.</param>
public sealed record Sugerencia(string Texto, TipoDeSugerencia Tipo, string? Detalle = null);
