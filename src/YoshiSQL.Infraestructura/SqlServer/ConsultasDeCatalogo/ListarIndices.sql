-- Una fila por cada columna de cada índice; se agrupan en el código
SELECT
    i.name AS NombreDelIndice,
    i.is_unique AS EsUnico,
    i.is_primary_key AS EsLlavePrimaria,
    CAST(IIF(i.type = 1, 1, 0) AS bit) AS EsAgrupado,
    c.name AS NombreDeColumna
FROM sys.indexes AS i
INNER JOIN sys.index_columns AS ic
    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns AS c
    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(QUOTENAME(@esquema) + '.' + QUOTENAME(@nombre))
  AND i.name IS NOT NULL
  AND ic.is_included_column = 0
ORDER BY i.name, ic.key_ordinal;
