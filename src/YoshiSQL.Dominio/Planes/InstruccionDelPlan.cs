namespace YoshiSQL.Dominio.Planes;

/// <param name="Raiz">Primera operación del plan; nula en instrucciones sin plan (ej. SET o DECLARE).</param>
public sealed record InstruccionDelPlan(string Texto, double CostoTotal, NodoDelPlan? Raiz, IReadOnlyList<string> Advertencias);
