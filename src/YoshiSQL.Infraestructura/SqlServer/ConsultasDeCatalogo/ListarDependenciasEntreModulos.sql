-- Qué vista o función usa a qué otra vista o función, para crearlas en un orden que no falle.
-- También qué tablas usan funciones (en la fórmula de una columna calculada): esas funciones van antes que las tablas.
SELECT DISTINCT
    esquemaDelDependiente.name AS EsquemaDelDependiente,
    dependiente.name AS NombreDelDependiente,
    esquemaDelRequerido.name AS EsquemaDelRequerido,
    requerido.name AS NombreDelRequerido
FROM sys.sql_expression_dependencies AS d
INNER JOIN sys.objects AS dependiente ON dependiente.object_id = d.referencing_id
INNER JOIN sys.schemas AS esquemaDelDependiente ON esquemaDelDependiente.schema_id = dependiente.schema_id
INNER JOIN sys.objects AS requerido ON requerido.object_id = d.referenced_id
INNER JOIN sys.schemas AS esquemaDelRequerido ON esquemaDelRequerido.schema_id = requerido.schema_id
WHERE dependiente.type IN ('V', 'FN', 'IF', 'TF', 'U')
  AND requerido.type IN ('V', 'FN', 'IF', 'TF')
  AND d.referencing_id <> d.referenced_id;
