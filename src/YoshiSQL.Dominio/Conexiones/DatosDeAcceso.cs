namespace YoshiSQL.Dominio.Conexiones;

/// <summary>
/// Perfil de conexión junto con la contraseña, listo para abrir una conexión.
/// Solo vive en memoria mientras la aplicación está abierta.
/// </summary>
public sealed class DatosDeAcceso
{
    public DatosDeAcceso(PerfilDeConexion perfil, string contrasena)
    {
        Perfil = perfil;
        Contrasena = contrasena;
    }

    public PerfilDeConexion Perfil { get; }

    public string Contrasena { get; }

    // Se sobrescribe para que la contraseña nunca aparezca en registros ni depuración
    public override string ToString() => $"{Perfil.Usuario}@{Perfil.NombreVisible}";
}
