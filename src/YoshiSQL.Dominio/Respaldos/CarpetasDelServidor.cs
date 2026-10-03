namespace YoshiSQL.Dominio.Respaldos;

/// <summary>
/// Carpetas predeterminadas que tiene configuradas la instancia de SQL Server.
/// </summary>
public sealed record CarpetasDelServidor(string Respaldos, string Datos, string Registros);
