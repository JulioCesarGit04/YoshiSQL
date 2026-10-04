namespace YoshiSQL.Infraestructura.Seguridad;

/// <summary>
/// Protege en disco la clave con la que se cifran las contraseñas guardadas.
/// Cada sistema operativo ofrece su propio mecanismo.
/// </summary>
internal interface IProtectorDeLaClave
{
    byte[] Proteger(byte[] clave);

    byte[] Desproteger(byte[] claveProtegida);
}
