-- Propiedades extendidas de tablas, vistas, procedimientos y funciones, y de sus columnas
-- (por ejemplo MS_Description, la descripción que documenta cada tabla y columna).
-- El valor se guarda en sql_variant; se devuelve como texto.
SELECT
    ep.name AS Nombre,
    CONVERT(nvarchar(4000), ep.value) AS Valor,
    s.name AS Esquema,
    o.name AS Objeto,
    CASE o.type WHEN 'U' THEN 'TABLE' WHEN 'V' THEN 'VIEW' WHEN 'P' THEN 'PROCEDURE' ELSE 'FUNCTION' END AS TipoDelObjeto,
    c.name AS Columna
FROM sys.extended_properties AS ep
INNER JOIN sys.objects AS o ON o.object_id = ep.major_id
INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
LEFT JOIN sys.columns AS c ON c.object_id = ep.major_id AND c.column_id = ep.minor_id AND ep.minor_id > 0
WHERE ep.class = 1
  AND o.is_ms_shipped = 0
  AND o.type IN ('U', 'V', 'P', 'FN', 'IF', 'TF')
  AND (ep.minor_id = 0 OR c.name IS NOT NULL)
  AND ep.value IS NOT NULL
  -- Marca interna de las herramientas de diagramas, no es del usuario
  AND ep.name <> N'microsoft_database_tools_support'
ORDER BY s.name, o.name, ep.minor_id, ep.name;
