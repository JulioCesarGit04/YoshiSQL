-- Valor inicial e incremento de cada columna IDENTITY (como texto: el catálogo los guarda en sql_variant)
SELECT
    s.name AS Esquema,
    t.name AS Tabla,
    ic.name AS Columna,
    CONVERT(nvarchar(60), ic.seed_value) AS Semilla,
    CONVERT(nvarchar(60), ic.increment_value) AS Incremento
FROM sys.identity_columns AS ic
INNER JOIN sys.tables AS t ON t.object_id = ic.object_id
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
  AND ic.seed_value IS NOT NULL
  AND ic.increment_value IS NOT NULL
ORDER BY s.name, t.name;
