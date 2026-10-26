namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Tipo de dato creado con CREATE TYPE ... FROM (alias de un tipo del sistema), ej. dbo.Telefono de nvarchar(25).
/// </summary>
public sealed record TipoDeUsuario(string Esquema, string Nombre, TipoDeDato TipoBase, bool AdmiteNulos);
