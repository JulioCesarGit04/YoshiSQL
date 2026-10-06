-- Disparadores (triggers) de una tabla
SELECT
    tr.name,
    tr.is_disabled
FROM sys.triggers tr
WHERE tr.parent_id = OBJECT_ID(QUOTENAME(@esquema) + N'.' + QUOTENAME(@nombre))
ORDER BY tr.name;
