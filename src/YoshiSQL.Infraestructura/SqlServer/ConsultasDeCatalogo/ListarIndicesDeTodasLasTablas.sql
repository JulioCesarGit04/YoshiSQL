-- Índices y restricciones UNIQUE de todas las tablas (sin la llave primaria, que viaja en el CREATE TABLE).
-- Una fila por columna; se agrupan en el código. Solo índices clásicos (agrupados y no agrupados):
-- los columnstore, XML, espaciales y de texto completo necesitan otra sintaxis.
SELECT
    s.name AS Esquema,
    t.name AS Tabla,
    i.name AS Nombre,
    i.is_unique AS EsUnico,
    i.is_unique_constraint AS EsRestriccionUnica,
    CAST(IIF(i.type = 1, 1, 0) AS bit) AS EsAgrupado,
    c.name AS Columna,
    ic.is_descending_key AS EsDescendente,
    ic.is_included_column AS EsIncluida,
    i.filter_definition AS Filtro
FROM sys.indexes AS i
INNER JOIN sys.tables AS t ON t.object_id = i.object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.is_ms_shipped = 0
  AND i.is_primary_key = 0
  AND i.is_hypothetical = 0
  AND i.type IN (1, 2)
  AND i.name IS NOT NULL
  -- En tablas particionadas la columna de partición aparece sin ser clave ni incluida: no es parte del índice
  AND (ic.key_ordinal > 0 OR ic.is_included_column = 1)
ORDER BY s.name, t.name, i.is_unique_constraint DESC, i.name, ic.is_included_column, ic.key_ordinal, ic.index_column_id;
