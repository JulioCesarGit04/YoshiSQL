-- Esquemas creados por el usuario: se excluyen dbo (1), guest (2), INFORMATION_SCHEMA (3), sys (4)
-- y los esquemas de los roles fijos de base de datos (16384 en adelante)
SELECT s.name AS Nombre
FROM sys.schemas AS s
WHERE s.schema_id > 4
  AND s.schema_id < 16384
ORDER BY s.name;
