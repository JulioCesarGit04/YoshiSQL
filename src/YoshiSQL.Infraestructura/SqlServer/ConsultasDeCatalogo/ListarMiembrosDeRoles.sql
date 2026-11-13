-- Quién pertenece a cada rol de la base. dbo no se incluye: es miembro de db_owner por definición
SELECT
    rol.name AS Rol,
    miembro.name AS Miembro
FROM sys.database_role_members AS rm
INNER JOIN sys.database_principals AS rol ON rol.principal_id = rm.role_principal_id
INNER JOIN sys.database_principals AS miembro ON miembro.principal_id = rm.member_principal_id
WHERE miembro.name <> N'dbo'
ORDER BY rol.name, miembro.name;
