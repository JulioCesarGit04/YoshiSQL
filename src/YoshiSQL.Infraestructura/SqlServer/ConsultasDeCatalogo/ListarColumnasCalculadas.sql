-- Fórmula de cada columna calculada
SELECT
    s.name AS Esquema,
    t.name AS Tabla,
    cc.name AS Columna,
    cc.definition AS Definicion,
    cc.is_persisted AS EsPersistida
FROM sys.computed_columns AS cc
INNER JOIN sys.tables AS t ON t.object_id = cc.object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, cc.column_id;
