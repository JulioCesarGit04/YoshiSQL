SELECT
    u.name AS Nombre,
    u.type_desc AS Tipo,
    l.name AS InicioDeSesion
FROM sys.database_principals AS u
LEFT JOIN sys.server_principals AS l ON l.sid = u.sid
WHERE u.type IN ('S', 'U', 'G', 'E', 'X')
  AND u.name NOT IN ('sys', 'INFORMATION_SCHEMA')
ORDER BY u.name;
