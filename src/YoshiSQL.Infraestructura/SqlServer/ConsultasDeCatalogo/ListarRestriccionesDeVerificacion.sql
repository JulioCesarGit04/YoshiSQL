-- Restricciones CHECK de todas las tablas. El nombre va NULL cuando lo generó SQL Server
SELECT
    s.name AS Esquema,
    t.name AS Tabla,
    IIF(cc.is_system_named = 1, NULL, cc.name) AS Nombre,
    cc.definition AS Definicion,
    cc.is_disabled AS EstaDeshabilitada
FROM sys.check_constraints AS cc
INNER JOIN sys.tables AS t ON t.object_id = cc.parent_object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, cc.name;
