-- Se excluyen las cuentas internas de SQL Server, cuyo nombre empieza con ##
SELECT
    p.name AS Nombre,
    p.type_desc AS Tipo,
    p.is_disabled AS EstaDeshabilitado
FROM sys.server_principals AS p
WHERE p.type IN ('S', 'U', 'G')
  AND p.name NOT LIKE '##%'
ORDER BY p.name;
