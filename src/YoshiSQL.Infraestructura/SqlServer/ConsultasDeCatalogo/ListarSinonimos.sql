-- Sinónimos (CREATE SYNONYM) con el objeto al que apuntan
SELECT
    s.name AS Esquema,
    sn.name AS Nombre,
    sn.base_object_name AS ObjetoBase
FROM sys.synonyms AS sn
INNER JOIN sys.schemas AS s ON s.schema_id = sn.schema_id
ORDER BY s.name, sn.name;
