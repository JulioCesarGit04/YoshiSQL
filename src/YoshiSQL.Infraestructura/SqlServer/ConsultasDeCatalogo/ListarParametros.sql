-- Parámetros de un procedimiento (el nombre incluye la @); se excluye el valor de retorno
SELECT
    p.name AS Nombre,
    ty.name AS Tipo,
    CAST(CASE
        WHEN ty.name IN ('varchar', 'char', 'varbinary', 'binary') THEN p.max_length
        WHEN ty.name IN ('nvarchar', 'nchar') THEN IIF(p.max_length = -1, -1, p.max_length / 2)
    END AS int) AS Longitud,
    CAST(IIF(ty.name IN ('decimal', 'numeric'), p.precision, NULL) AS int) AS Precision,
    CAST(IIF(ty.name IN ('decimal', 'numeric'), p.scale, NULL) AS int) AS Escala,
    p.is_output AS EsSalida
FROM sys.parameters AS p
INNER JOIN sys.types AS ty ON ty.user_type_id = p.user_type_id
WHERE p.object_id = OBJECT_ID(QUOTENAME(@esquema) + '.' + QUOTENAME(@nombre))
    AND p.parameter_id > 0
ORDER BY p.parameter_id;
