namespace YoshiSQL.Dominio.Planes;

/// <summary>
/// Una operación del plan de ejecución (ej. Index Seek, Hash Match) con su costo y sus operaciones hijas.
/// </summary>
/// <param name="PorcentajeDelCosto">Parte del costo total de la instrucción que corresponde solo a esta operación.</param>
/// <param name="FilasReales">Solo en planes reales: filas que realmente devolvió la operación.</param>
public sealed record NodoDelPlan(
    string OperacionFisica,
    string OperacionLogica,
    string? Objeto,
    double PorcentajeDelCosto,
    double FilasEstimadas,
    long? FilasReales,
    long? Ejecuciones,
    IReadOnlyList<string> Advertencias,
    IReadOnlyList<NodoDelPlan> Hijos)
{
    public bool TieneAdvertencias => Advertencias.Count > 0;

    /// <summary>
    /// Una diferencia grande entre filas estimadas y reales suele indicar estadísticas desactualizadas.
    /// La estimación es por ejecución, así que se compara con las filas reales por ejecución
    /// (un Key Lookup que se ejecuta 50 veces con 1 fila cada vez está bien estimado).
    /// </summary>
    public bool EstimacionMuyDiferente
    {
        get
        {
            if (FilasReales is not long reales)
            {
                return false;
            }

            var realesPorEjecucion = (double)reales / Math.Max(1, Ejecuciones ?? 1);
            return Math.Max(realesPorEjecucion, FilasEstimadas) >= 10 * Math.Max(1, Math.Min(realesPorEjecucion, FilasEstimadas));
        }
    }
}
