namespace YoshiSQL.Dominio.Autocompletado;

/// <summary>
/// Qué se está escribiendo en la posición del cursor.
/// </summary>
/// <param name="PalabraParcial">Lo que ya se escribió de la palabra actual, ej. "Cli".</param>
/// <param name="Calificador">El texto antes del punto, ej. "c" en "c.Nom"; nulo si no hay punto.</param>
/// <param name="TablasDeLaInstruccion">Tablas mencionadas en el lote actual, con sus alias.</param>
public sealed record ContextoDeAutocompletado(
    TipoDeContexto Tipo,
    string PalabraParcial,
    string? Calificador,
    IReadOnlyList<ReferenciaDeTabla> TablasDeLaInstruccion)
{
    public static readonly ContextoDeAutocompletado SinSugerencias =
        new(TipoDeContexto.SinSugerencias, string.Empty, null, []);
}
