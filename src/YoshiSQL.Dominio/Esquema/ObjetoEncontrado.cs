namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Un resultado de "buscar en la base de datos".
/// </summary>
/// <param name="Columna">Columna que coincidió, cuando lo encontrado es una columna y no el objeto.</param>
public sealed record ObjetoEncontrado(ObjetoDeEsquema Objeto, string? Columna, TipoDeCoincidencia Coincidencia);
