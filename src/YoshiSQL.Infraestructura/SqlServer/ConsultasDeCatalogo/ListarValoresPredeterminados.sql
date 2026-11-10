-- Restricciones DEFAULT de todas las tablas. El nombre va NULL cuando lo generó SQL Server (DF__Tabla__Col__1A2B3C)
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
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;
