-- Disparadores de tablas y vistas con su código. La definición es NULL si está cifrado o no hay permiso.
-- Los disparadores de base de datos (DDL) y los de ensamblados CLR no se incluyen.
SELECT
    s.name AS Esquema,
    tr.name AS Nombre,
    esquemaDelObjeto.name AS EsquemaDelObjeto,
    objeto.name AS NombreDelObjeto,
    OBJECT_DEFINITION(tr.object_id) AS Definicion,
    tr.is_disabled AS EstaDeshabilitado
FROM sys.triggers AS tr
INNER JOIN sys.objects AS o ON o.object_id = tr.object_id
INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
INNER JOIN sys.objects AS objeto ON objeto.object_id = tr.parent_id
INNER JOIN sys.schemas AS esquemaDelObjeto ON esquemaDelObjeto.schema_id = objeto.schema_id
WHERE tr.parent_class = 1
  AND tr.is_ms_shipped = 0
  AND tr.type = 'TR'
ORDER BY esquemaDelObjeto.name, objeto.name, tr.name;
