-- Columnas de los tipos tabla (CREATE TYPE ... AS TABLE).
-- Mismas columnas que ListarColumnasDeTodasLasTablas.sql, precedidas por el esquema y el nombre del tipo
SELECT
    s.name AS Esquema,
    tt.name AS Tipo,
    c.name AS Nombre,
    -- Un tipo de usuario que no está en dbo necesita su esquema (ventas.Codigo); sin él no se encuentra
    IIF(ty.is_user_defined = 1 AND SCHEMA_NAME(ty.schema_id) <> N'dbo', SCHEMA_NAME(ty.schema_id) + N'.' + ty.name, ty.name) AS TipoDeLaColumna,
    CAST(CASE
        WHEN ty.name IN ('varchar', 'char', 'varbinary', 'binary') THEN c.max_length
        WHEN ty.name IN ('nvarchar', 'nchar') THEN IIF(c.max_length = -1, -1, c.max_length / 2)
        WHEN ty.name IN ('datetime2', 'time', 'datetimeoffset') THEN c.scale
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
    c.column_id AS Posicion,
    c.is_computed AS EsCalculada
FROM sys.table_types AS tt
INNER JOIN sys.schemas AS s ON s.schema_id = tt.schema_id
INNER JOIN sys.columns AS c ON c.object_id = tt.type_table_object_id
INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
WHERE tt.is_user_defined = 1
ORDER BY s.name, tt.name, c.column_id;
