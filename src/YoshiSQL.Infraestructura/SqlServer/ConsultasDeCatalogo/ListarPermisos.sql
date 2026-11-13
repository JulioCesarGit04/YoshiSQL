-- Permisos concedidos o denegados a usuarios y roles: de toda la base (clase 0),
-- de un objeto o una de sus columnas (clase 1) y de un esquema (clase 3).
-- No se incluyen los de los usuarios internos ni los que SQL Server da a "public" sobre sus propios objetos.
SELECT
    CAST(IIF(p.state = 'D', 1, 0) AS bit) AS EsDenegacion,
    p.permission_name AS Permiso,
    CASE p.class WHEN 1 THEN OBJECT_SCHEMA_NAME(p.major_id) WHEN 3 THEN SCHEMA_NAME(p.major_id) END AS Esquema,
    CASE p.class WHEN 1 THEN OBJECT_NAME(p.major_id) END AS Objeto,
    c.name AS Columna,
    beneficiario.name AS Beneficiario,
    CAST(IIF(p.state = 'W', 1, 0) AS bit) AS PuedeOtorgarlo
FROM sys.database_permissions AS p
INNER JOIN sys.database_principals AS beneficiario ON beneficiario.principal_id = p.grantee_principal_id
LEFT JOIN sys.objects AS o ON p.class = 1 AND o.object_id = p.major_id
LEFT JOIN sys.columns AS c ON p.class = 1 AND c.object_id = p.major_id AND c.column_id = p.minor_id AND p.minor_id > 0
WHERE p.class IN (0, 1, 3)
  AND p.state IN ('G', 'W', 'D')
  AND beneficiario.name NOT IN (N'dbo', N'sys', N'INFORMATION_SCHEMA', N'guest')
  AND (p.class <> 1 OR (o.is_ms_shipped = 0 AND o.type IN ('U', 'V', 'P', 'FN', 'IF', 'TF')))
  AND (p.class <> 1 OR p.minor_id = 0 OR c.name IS NOT NULL)
  -- Los permisos de toda la base que tiene "public" son los que SQL Server pone en cualquier base nueva
  AND NOT (p.class = 0 AND beneficiario.name = N'public')
  -- CONNECT se concede solo al crear el usuario
  AND NOT (p.class = 0 AND p.permission_name = N'CONNECT' AND p.state = 'G')
ORDER BY beneficiario.name, p.class, Esquema, Objeto, p.permission_name;
