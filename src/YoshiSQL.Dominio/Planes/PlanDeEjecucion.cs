namespace YoshiSQL.Dominio.Planes;

/// <param name="EsReal">Verdadero si se ejecutó la consulta y el plan incluye filas reales.</param>
/// <param name="DocumentosXml">XML original, para copiarlo a otras herramientas.</param>
public sealed record PlanDeEjecucion(
    IReadOnlyList<InstruccionDelPlan> Instrucciones,
    bool EsReal,
    IReadOnlyList<string> DocumentosXml);
