-- Mismas columnas que ListarColumnas.sql, precedidas por el esquema y la tabla
SELECT
    s.name AS Esquema,
    t.name AS Tabla,
    c.name AS Nombre,
    ty.name AS Tipo,
    CAST(CASE
        WHEN ty.name IN ('varchar', 'char', 'varbinary', 'binary') THEN c.max_length
        WHEN ty.name IN ('nvarchar', 'nchar') THEN IIF(c.max_length = -1, -1, c.max_length / 2)
    END AS int) AS Longitud,
    CAST(IIF(ty.name IN ('decimal', 'numeric'), c.precision, NULL) AS int) AS Precision,
    CAST(IIF(ty.name IN ('decimal', 'numeric'), c.scale, NULL) AS int) AS Escala,
    c.is_nullable AS AdmiteNulos,
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM sys.indexes AS i
        INNER JOIN sys.index_columns AS ic
            ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        WHERE i.is_primary_key = 1
          AND ic.object_id = c.object_id
          AND ic.column_id = c.column_id
    ) THEN 1 ELSE 0 END AS bit) AS EsLlavePrimaria,
    c.is_identity AS EsIdentidad,
    c.column_id AS Posicion
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN sys.columns AS c ON c.object_id = t.object_id
INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;
