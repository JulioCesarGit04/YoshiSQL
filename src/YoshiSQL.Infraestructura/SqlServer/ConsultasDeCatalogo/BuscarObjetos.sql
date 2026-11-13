-- Busca un texto en toda la base: en el nombre de los objetos, en el nombre de las columnas
-- y dentro del código de procedimientos, funciones, vistas y disparadores.
-- @patron llega ya con los comodines (%texto%). Coincidencia: 1 = nombre, 2 = columna, 3 = código.
SELECT TOP (300)
    encontrados.Tipo,
    encontrados.Esquema,
    encontrados.Nombre,
    encontrados.Columna,
    encontrados.Coincidencia
FROM (
    SELECT RTRIM(o.type) AS Tipo, s.name AS Esquema, o.name AS Nombre, CAST(NULL AS sysname) AS Columna, 1 AS Coincidencia
    FROM sys.objects AS o
    INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
    WHERE o.is_ms_shipped = 0
      AND o.type IN ('U', 'V', 'P', 'FN', 'IF', 'TF')
      AND o.name LIKE @patron

    UNION ALL

    SELECT RTRIM(o.type), s.name, o.name, c.name, 2
    FROM sys.columns AS c
    INNER JOIN sys.objects AS o ON o.object_id = c.object_id
    INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
    WHERE o.is_ms_shipped = 0
      AND o.type IN ('U', 'V')
      AND c.name LIKE @patron

    UNION ALL

    SELECT RTRIM(o.type), s.name, o.name, NULL, 3
    FROM sys.sql_modules AS m
    INNER JOIN sys.objects AS o ON o.object_id = m.object_id
    INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
    WHERE o.is_ms_shipped = 0
      AND o.type IN ('V', 'P', 'FN', 'IF', 'TF')
      AND m.definition LIKE @patron
      -- Si el nombre ya coincide, el objeto ya salió arriba
      AND o.name NOT LIKE @patron
) AS encontrados
ORDER BY encontrados.Coincidencia, encontrados.Esquema, encontrados.Nombre, encontrados.Columna;
