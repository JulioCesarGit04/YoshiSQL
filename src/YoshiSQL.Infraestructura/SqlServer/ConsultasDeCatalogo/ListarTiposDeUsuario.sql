-- Tipos alias (CREATE TYPE ... FROM). Los tipos tabla y los de ensamblados CLR se manejan aparte.
-- Las longitudes de nchar/nvarchar se guardan en bytes; se dividen entre 2 para mostrar caracteres
SELECT
    s.name AS Esquema,
    t.name AS Nombre,
    tipoBase.name AS TipoBase,
    CAST(CASE
        WHEN tipoBase.name IN ('varchar', 'char', 'varbinary', 'binary') THEN t.max_length
        WHEN tipoBase.name IN ('nvarchar', 'nchar') THEN IIF(t.max_length = -1, -1, t.max_length / 2)
        -- Precisión de fracciones de segundo: datetime2(3), time(0)... sin ella se recreaban con la predeterminada (7)
        WHEN tipoBase.name IN ('datetime2', 'time', 'datetimeoffset') THEN t.scale
    END AS int) AS Longitud,
    CAST(IIF(tipoBase.name IN ('decimal', 'numeric'), t.precision, NULL) AS int) AS Precision,
    CAST(IIF(tipoBase.name IN ('decimal', 'numeric'), t.scale, NULL) AS int) AS Escala,
    t.is_nullable AS AdmiteNulos
FROM sys.types AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
INNER JOIN sys.types AS tipoBase ON tipoBase.user_type_id = t.system_type_id
WHERE t.is_user_defined = 1
  AND t.is_table_type = 0
  AND t.is_assembly_type = 0
ORDER BY s.name, t.name;
