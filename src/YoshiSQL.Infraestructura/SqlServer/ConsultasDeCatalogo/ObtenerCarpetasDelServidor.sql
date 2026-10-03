SELECT
    CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(512)) AS Respaldos,
    CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(512)) AS Datos,
    CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(512)) AS Registros;
