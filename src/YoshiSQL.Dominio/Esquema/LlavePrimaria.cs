namespace YoshiSQL.Dominio.Esquema;

public sealed record LlavePrimaria(string Nombre, IReadOnlyList<string> Columnas);
