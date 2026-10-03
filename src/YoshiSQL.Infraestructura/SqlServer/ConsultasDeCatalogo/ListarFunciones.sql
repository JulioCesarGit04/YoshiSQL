SELECT s.name AS Esquema, o.name AS Nombre
FROM sys.objects AS o
INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
WHERE o.type IN ('FN', 'IF', 'TF', 'FS', 'FT')
  AND o.is_ms_shipped = 0
ORDER BY s.name, o.name;
