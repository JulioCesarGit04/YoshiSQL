SELECT
    d.name AS Nombre,
    CAST(CASE WHEN d.state_desc = 'ONLINE' THEN 1 ELSE 0 END AS bit) AS EstaEnLinea,
    CAST(CASE WHEN d.database_id <= 4 THEN 1 ELSE 0 END AS bit) AS EsDelSistema
FROM sys.databases AS d
ORDER BY d.name;
