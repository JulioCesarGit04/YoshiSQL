namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Índice o restricción UNIQUE de una tabla con todo lo necesario para volver a crearlo.
/// La llave primaria no se incluye: viaja dentro del CREATE TABLE.
/// </summary>
/// <param name="EsRestriccionUnica">Verdadero si se creó como CONSTRAINT ... UNIQUE (una llave foránea puede apuntar a ella).</param>
/// <param name="ColumnasIncluidas">Columnas de la cláusula INCLUDE.</param>
/// <param name="Filtro">Condición de un índice filtrado (WHERE), o nulo.</param>
/// <param name="EsColumnar">
/// Índice columnstore (guarda los datos por columna, para análisis). Sus columnas llegan todas como "incluidas";
/// el agrupado no lista columnas porque abarca la tabla entera.
/// </param>
public sealed record IndiceDeTabla(
    Tabla Tabla,
    string Nombre,
    bool EsUnico,
    bool EsRestriccionUnica,
    bool EsAgrupado,
    IReadOnlyList<ColumnaDeIndice> Columnas,
    IReadOnlyList<string> ColumnasIncluidas,
    string? Filtro,
    bool EsColumnar = false);
