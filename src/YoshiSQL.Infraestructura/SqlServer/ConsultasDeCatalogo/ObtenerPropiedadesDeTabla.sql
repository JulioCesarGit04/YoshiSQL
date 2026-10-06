-- Propiedades de una tabla: fechas, filas, espacio en disco (KB) y conteos de columnas e índices
SELECT
    t.create_date,
    t.modify_date,
    ISNULL(SUM(CASE WHEN ps.index_id IN (0, 1) THEN ps.row_count END), 0) AS filas,
    ISNULL(SUM(ps.reserved_page_count), 0) * 8 AS espacioTotalKb,
    ISNULL(SUM(ps.used_page_count), 0) * 8 AS espacioUsadoKb,
    (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS columnas,
    (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id AND i.index_id > 0) AS indices
FROM sys.tables t
LEFT JOIN sys.dm_db_partition_stats ps ON ps.object_id = t.object_id
WHERE t.object_id = OBJECT_ID(QUOTENAME(@esquema) + '.' + QUOTENAME(@nombre))
GROUP BY t.create_date, t.modify_date, t.object_id;
