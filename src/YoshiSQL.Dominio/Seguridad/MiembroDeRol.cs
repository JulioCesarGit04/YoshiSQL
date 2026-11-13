namespace YoshiSQL.Dominio.Seguridad;

/// <summary>
/// Un usuario (o un rol) que pertenece a un rol de la base de datos.
/// </summary>
public sealed record MiembroDeRol(string Rol, string Miembro);
