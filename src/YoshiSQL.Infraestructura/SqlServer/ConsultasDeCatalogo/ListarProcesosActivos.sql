-- Sesiones de usuario con lo que están ejecutando ahora (si ejecutan algo) y quién las bloquea
SELECT
    s.session_id AS IdDeSesion,
    s.login_name AS InicioDeSesion,
    ISNULL(s.host_name, '') AS Equipo,
    ISNULL(s.program_name, '') AS Programa,
    ISNULL(DB_NAME(COALESCE(r.database_id, s.database_id)), '') AS BaseDeDatos,
    COALESCE(r.status, s.status) AS Estado,
    r.command AS Comando,
    r.wait_type AS TipoDeEspera,
    NULLIF(r.blocking_session_id, 0) AS BloqueadoPorSesion,
    CAST(ISNULL(r.total_elapsed_time, 0) AS bigint) AS TiempoTranscurrido,
    CAST(COALESCE(r.cpu_time, s.cpu_time) AS bigint) AS TiempoDeCpu,
    CAST(COALESCE(r.logical_reads, s.logical_reads) AS bigint) AS Lecturas,
    SUBSTRING(
        t.text,
        (r.statement_start_offset / 2) + 1,
        ((IIF(r.statement_end_offset = -1, DATALENGTH(t.text), r.statement_end_offset) - r.statement_start_offset) / 2) + 1
    ) AS TextoSql
FROM sys.dm_exec_sessions AS s
LEFT JOIN sys.dm_exec_requests AS r ON r.session_id = s.session_id
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) AS t
WHERE s.is_user_process = 1
  AND s.session_id <> @@SPID
ORDER BY IIF(r.blocking_session_id > 0, 0, 1), IIF(r.session_id IS NULL, 1, 0), s.session_id;
