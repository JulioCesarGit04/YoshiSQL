SELECT
    r.name AS Nombre,
    r.is_fixed_role AS EsFijo
FROM sys.database_principals AS r
WHERE r.type = 'R'
ORDER BY r.is_fixed_role DESC, r.name;
