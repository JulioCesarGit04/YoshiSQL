-- Cifras generales de la base de datos actual
SELECT
    (SELECT COUNT(*) FROM sys.tables) AS tablas,
    ISNULL((SELECT SUM(ps.row_count) FROM sys.dm_db_partition_stats ps
            JOIN sys.tables t ON t.object_id = ps.object_id
            WHERE ps.index_id IN (0, 1)), 0) AS filas,
    (SELECT ISNULL(SUM(CAST(mf.size AS bigint)), 0) * 8
     FROM sys.master_files mf WHERE mf.database_id = DB_ID()) AS tamanoKb,
    ISNULL((SELECT SUM(ps.reserved_page_count) FROM sys.dm_db_partition_stats ps), 0) * 8 AS espacioUsadoKb;
