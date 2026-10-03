-- Mismas columnas que ListarColumnas.sql, precedidas por el esquema y la vista
SELECT
    s.name AS Esquema,
    v.name AS Vista,
    c.name AS Nombre,
    ty.name AS Tipo,
    CAST(CASE
        WHEN ty.name IN ('varchar', 'char', 'varbinary', 'binary') THEN c.max_length
        WHEN ty.name IN ('nvarchar', 'nchar') THEN IIF(c.max_length = -1, -1, c.max_length / 2)
    END AS int) AS Longitud,
    CAST(IIF(ty.name IN ('decimal', 'numeric'), c.precision, NULL) AS int) AS Precision,
    CAST(IIF(ty.name IN ('decimal', 'numeric'), c.scale, NULL) AS int) AS Escala,
    c.is_nullable AS AdmiteNulos,
    CAST(0 AS bit) AS EsLlavePrimaria,
    c.is_identity AS EsIdentidad,
    c.column_id AS Posicion
FROM sys.views AS v
INNER JOIN sys.schemas AS s ON s.schema_id = v.schema_id
INNER JOIN sys.columns AS c ON c.object_id = v.object_id
INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
WHERE v.is_ms_shipped = 0
ORDER BY s.name, v.name, c.column_id;
