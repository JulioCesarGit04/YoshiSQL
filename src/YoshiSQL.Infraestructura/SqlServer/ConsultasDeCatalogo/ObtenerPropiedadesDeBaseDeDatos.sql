-- Propiedades de una base de datos: estado, recuperación, intercalación, compatibilidad, propietario y tamaño (KB)
SELECT
    d.name,
    d.state_desc,
    d.recovery_model_desc,
    d.collation_name,
    d.compatibility_level,
    SUSER_SNAME(d.owner_sid) AS propietario,
    d.create_date,
    (SELECT ISNULL(SUM(CAST(mf.size AS bigint)), 0) * 8
     FROM sys.master_files mf
     WHERE mf.database_id = d.database_id) AS tamanoKb
FROM sys.databases d
WHERE d.name = @nombre;
