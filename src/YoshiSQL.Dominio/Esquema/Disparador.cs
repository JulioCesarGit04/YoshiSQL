namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Un disparador (trigger) de una tabla.
/// </summary>
public sealed record Disparador(string Nombre, bool EstaDeshabilitado);
