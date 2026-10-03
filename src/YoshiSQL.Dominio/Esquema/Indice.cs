namespace YoshiSQL.Dominio.Esquema;

public sealed record Indice(
    string Nombre,
    bool EsUnico,
    bool EsLlavePrimaria,
    bool EsAgrupado,
    IReadOnlyList<string> Columnas);
