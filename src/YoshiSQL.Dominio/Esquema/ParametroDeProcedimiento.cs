namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Un parámetro de un procedimiento almacenado. El nombre incluye la @.
/// </summary>
public sealed record ParametroDeProcedimiento(string Nombre, TipoDeDato TipoDeDato, bool EsSalida);
