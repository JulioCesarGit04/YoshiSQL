-- Secuencias (CREATE SEQUENCE). Los valores se devuelven como texto porque el catálogo los guarda en sql_variant.
-- SiguienteValor: si la secuencia ya se usó, el valor actual más el incremento; así la copia no repite números.
SELECT
    s.name AS Esquema,
    sq.name AS Nombre,
    IIF(ty.name IN ('decimal', 'numeric'), ty.name + '(' + CAST(sq.precision AS varchar(3)) + ',0)', ty.name) AS TipoDeDato,
    CONVERT(nvarchar(60), IIF(
        sq.last_used_value IS NULL,
        CONVERT(decimal(38, 0), sq.current_value),
        CONVERT(decimal(38, 0), sq.current_value) + CONVERT(decimal(38, 0), sq.increment))) AS SiguienteValor,
    CONVERT(nvarchar(60), sq.increment) AS Incremento,
    CONVERT(nvarchar(60), sq.minimum_value) AS ValorMinimo,
    CONVERT(nvarchar(60), sq.maximum_value) AS ValorMaximo,
    sq.is_cycling AS EsCiclica,
    sq.is_cached AS UsaCache,
    sq.cache_size AS TamanoDeCache
FROM sys.sequences AS sq
INNER JOIN sys.schemas AS s ON s.schema_id = sq.schema_id
INNER JOIN sys.types AS ty ON ty.user_type_id = sq.system_type_id
ORDER BY s.name, sq.name;
