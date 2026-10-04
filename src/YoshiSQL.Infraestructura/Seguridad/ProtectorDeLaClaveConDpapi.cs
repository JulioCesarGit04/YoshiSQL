using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace YoshiSQL.Infraestructura.Seguridad;

/// <summary>
/// En Windows la clave se cifra con DPAPI, el servicio de protección de datos del sistema:
/// solo la misma cuenta de usuario, en el mismo equipo, puede volver a leerla.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class ProtectorDeLaClaveConDpapi : IProtectorDeLaClave
{
    // Dato adicional que distingue esta clave de otras protegidas por la misma cuenta
    private static readonly byte[] EntropiaDeYoshiSql = Encoding.UTF8.GetBytes("YoshiSQL.ClaveDeCredenciales");

    public byte[] Proteger(byte[] clave) =>
        ProtectedData.Protect(clave, EntropiaDeYoshiSql, DataProtectionScope.CurrentUser);

    public byte[] Desproteger(byte[] claveProtegida) =>
        ProtectedData.Unprotect(claveProtegida, EntropiaDeYoshiSql, DataProtectionScope.CurrentUser);
}
