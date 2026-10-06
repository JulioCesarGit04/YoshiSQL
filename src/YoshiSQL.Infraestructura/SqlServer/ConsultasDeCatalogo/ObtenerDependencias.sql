-- Objetos de los que depende el objeto (a los que hace referencia) y objetos que lo usan
SELECT DISTINCT
    ISNULL(OBJECT_SCHEMA_NAME(d.referenced_id), d.referenced_schema_name) AS esquema,
    ISNULL(OBJECT_NAME(d.referenced_id), d.referenced_entity_name) AS nombre,
    N'Depende de' AS relacion
FROM sys.sql_expression_dependencies d
WHERE d.referencing_id = OBJECT_ID(QUOTENAME(@esquema) + N'.' + QUOTENAME(@nombre))
    AND COALESCE(OBJECT_NAME(d.referenced_id), d.referenced_entity_name) IS NOT NULL

UNION

SELECT DISTINCT
    OBJECT_SCHEMA_NAME(d.referencing_id) AS esquema,
    OBJECT_NAME(d.referencing_id) AS nombre,
    N'Lo usa' AS relacion
FROM sys.sql_expression_dependencies d
WHERE d.referenced_id = OBJECT_ID(QUOTENAME(@esquema) + N'.' + QUOTENAME(@nombre))

ORDER BY relacion, esquema, nombre;
