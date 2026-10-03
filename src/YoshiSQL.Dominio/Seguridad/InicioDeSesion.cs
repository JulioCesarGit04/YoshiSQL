namespace YoshiSQL.Dominio.Seguridad;

/// <summary>
/// Cuenta con la que se entra al servidor (LOGIN).
/// </summary>
/// <param name="Tipo">Autenticación de SQL Server, Windows o grupo de Windows.</param>
public sealed record InicioDeSesion(string Nombre, string Tipo, bool EstaDeshabilitado);
