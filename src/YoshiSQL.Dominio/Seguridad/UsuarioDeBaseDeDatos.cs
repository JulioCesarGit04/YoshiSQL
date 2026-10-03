namespace YoshiSQL.Dominio.Seguridad;

/// <summary>
/// Usuario dentro de una base de datos (USER), normalmente asociado a un inicio de sesión.
/// </summary>
public sealed record UsuarioDeBaseDeDatos(string Nombre, string Tipo, string? InicioDeSesion);
