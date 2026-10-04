namespace YoshiSQL.Infraestructura.Seguridad;

/// <summary>
/// En Linux la clave se guarda tal cual y la protección la dan los permisos del archivo
/// (600: solo el usuario puede leerlo), que aplica el escritor de archivos seguro.
/// </summary>
internal sealed class ProtectorDeLaClaveConPermisosDeArchivo : IProtectorDeLaClave
{
    public byte[] Proteger(byte[] clave) => clave;

    public byte[] Desproteger(byte[] claveProtegida) => claveProtegida;
}
