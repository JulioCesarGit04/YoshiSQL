SELECT s.name AS Esquema, v.name AS Nombre
FROM sys.views AS v
INNER JOIN sys.schemas AS s ON s.schema_id = v.schema_id
WHERE v.is_ms_shipped = 0
ORDER BY s.name, v.name;
