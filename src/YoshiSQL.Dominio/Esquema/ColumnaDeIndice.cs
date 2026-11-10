namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Columna que forma parte de la clave de un índice, con su sentido de ordenamiento.
/// </summary>
public sealed record ColumnaDeIndice(string Nombre, bool EsDescendente);
