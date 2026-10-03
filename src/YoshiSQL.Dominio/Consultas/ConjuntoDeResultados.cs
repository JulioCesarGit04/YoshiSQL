namespace YoshiSQL.Dominio.Consultas;

/// <summary>
/// Una tabla de resultados devuelta por un SELECT.
/// </summary>
public sealed record ConjuntoDeResultados(
    IReadOnlyList<ColumnaDeResultado> Columnas,
    IReadOnlyList<object?[]> Filas)
{
    public int CantidadDeFilas => Filas.Count;
}
