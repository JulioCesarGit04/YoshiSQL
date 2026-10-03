-- Una fila por cada par de columnas de cada llave foránea; se agrupan en el código
SELECT
    fk.name AS NombreDeLaLlave,
    esquemaOrigen.name AS EsquemaOrigen,
    tablaOrigen.name AS TablaOrigen,
    columnaOrigen.name AS ColumnaOrigen,
    esquemaDestino.name AS EsquemaDestino,
    tablaDestino.name AS TablaDestino,
    columnaDestino.name AS ColumnaDestino
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.tables AS tablaOrigen ON tablaOrigen.object_id = fk.parent_object_id
INNER JOIN sys.schemas AS esquemaOrigen ON esquemaOrigen.schema_id = tablaOrigen.schema_id
INNER JOIN sys.columns AS columnaOrigen
    ON columnaOrigen.object_id = fkc.parent_object_id AND columnaOrigen.column_id = fkc.parent_column_id
INNER JOIN sys.tables AS tablaDestino ON tablaDestino.object_id = fk.referenced_object_id
INNER JOIN sys.schemas AS esquemaDestino ON esquemaDestino.schema_id = tablaDestino.schema_id
INNER JOIN sys.columns AS columnaDestino
    ON columnaDestino.object_id = fkc.referenced_object_id AND columnaDestino.column_id = fkc.referenced_column_id
ORDER BY fk.name, fkc.constraint_column_id;
