-- Las 10 tablas que más espacio ocupan
SELECT TOP (10)
    s.name + '.' + t.name AS tabla,
    ISNULL(SUM(CASE WHEN ps.index_id IN (0, 1) THEN ps.row_count END), 0) AS filas,
    ISNULL(SUM(ps.reserved_page_count), 0) * 8 AS espacioKb
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.dm_db_partition_stats ps ON ps.object_id = t.object_id
GROUP BY s.name, t.name
ORDER BY espacioKb DESC;
