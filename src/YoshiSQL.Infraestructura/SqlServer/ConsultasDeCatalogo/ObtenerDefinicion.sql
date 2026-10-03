-- Devuelve NULL si el objeto está cifrado (WITH ENCRYPTION) o el usuario no tiene permiso VIEW DEFINITION
SELECT OBJECT_DEFINITION(OBJECT_ID(QUOTENAME(@esquema) + '.' + QUOTENAME(@nombre))) AS Definicion;
