namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Tipo tabla (CREATE TYPE ... AS TABLE), el que usan los procedimientos para recibir listas de filas.
/// </summary>
public sealed record TipoDeTabla(string Esquema, string Nombre, IReadOnlyList<Columna> Columnas)
{
    public string NombreCompleto => $"{Esquema}.{Nombre}";
}
