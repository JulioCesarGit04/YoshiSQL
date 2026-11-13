namespace YoshiSQL.Dominio.Seguridad;

/// <summary>
/// Quién puede hacer qué dentro de una base: sus usuarios, sus roles propios, quién pertenece a cada rol y los permisos.
/// </summary>
public sealed record SeguridadDeLaBaseDeDatos(
    IReadOnlyList<UsuarioDeBaseDeDatos> Usuarios,
    IReadOnlyList<RolDeBaseDeDatos> Roles,
    IReadOnlyList<MiembroDeRol> Miembros,
    IReadOnlyList<PermisoDeBaseDeDatos> Permisos);
