SELECT
    COALESCE(@@SERVERNAME, CAST(SERVERPROPERTY('ServerName') AS nvarchar(256))) AS Nombre,
    CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)) AS Version,
    CAST(SERVERPROPERTY('Edition') AS nvarchar(128)) AS Edicion;
