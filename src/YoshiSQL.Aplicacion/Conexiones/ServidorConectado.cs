using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Conexiones;

/// <summary>
/// Servidor con el que se estableció conexión correctamente durante esta sesión de la aplicación.
/// </summary>
public sealed record ServidorConectado(DatosDeAcceso DatosDeAcceso, Servidor Servidor)
{
    public PerfilDeConexion Perfil => DatosDeAcceso.Perfil;
}
