-- Restricciones DEFAULT de una tabla (para el diseñador). Mismas columnas que ListarValoresPredeterminados.sql
SELECT
    s.name AS Esquema,
    t.name AS Tabla,
    c.name AS Columna,
    IIF(dc.is_system_named = 1, NULL, dc.name) AS Nombre,
    dc.definition AS Definicion
FROM sys.default_constraints AS dc
INNER JOIN sys.tables AS t ON t.object_id = dc.parent_object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(QUOTENAME(@esquema) + '.' + QUOTENAME(@nombre))
ORDER BY c.column_id;
