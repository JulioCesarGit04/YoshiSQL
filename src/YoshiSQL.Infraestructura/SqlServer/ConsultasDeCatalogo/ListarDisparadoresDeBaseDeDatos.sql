-- Disparadores de base de datos (DDL: reaccionan a CREATE, ALTER, DROP...) con su código.
-- La definición es NULL si está cifrado o no hay permiso. Los de ensamblados CLR no se incluyen.
SELECT
    tr.name AS Nombre,
    OBJECT_DEFINITION(tr.object_id) AS Definicion,
    tr.is_disabled AS EstaDeshabilitado
FROM sys.triggers AS tr
WHERE tr.parent_class = 0
  AND tr.is_ms_shipped = 0
  AND tr.type = 'TR'
ORDER BY tr.name;
