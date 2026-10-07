-- Los 10 índices más fragmentados (ignorando los muy pequeños)
SELECT TOP (10)
    s.name + '.' + t.name AS tabla,
    i.name AS indice,
    estadisticas.avg_fragmentation_in_percent AS fragmentacion,
    estadisticas.page_count AS paginas
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') AS estadisticas
JOIN sys.tables t ON t.object_id = estadisticas.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.indexes i ON i.object_id = estadisticas.object_id AND i.index_id = estadisticas.index_id
WHERE i.index_id > 0 AND estadisticas.page_count >= 100 AND estadisticas.avg_fragmentation_in_percent >= 5
ORDER BY estadisticas.avg_fragmentation_in_percent DESC;
