-- Una base en línea a la que el usuario no tiene acceso se trata como no disponible:
-- si no, se podía desplegar en el árbol y cada carpeta daba un error
SELECT
    d.name AS Nombre,
    CAST(CASE WHEN d.state_desc = 'ONLINE' AND HAS_DBACCESS(d.name) = 1 THEN 1 ELSE 0 END AS bit) AS EstaEnLinea,
    CAST(CASE WHEN d.database_id <= 4 THEN 1 ELSE 0 END AS bit) AS EsDelSistema
FROM sys.databases AS d
ORDER BY d.name;
