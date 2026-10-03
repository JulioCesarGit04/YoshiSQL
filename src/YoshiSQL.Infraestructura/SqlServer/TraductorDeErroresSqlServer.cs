using Microsoft.Data.SqlClient;
using YoshiSQL.Dominio.Errores;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Convierte los errores técnicos de SqlClient en errores del dominio con mensajes claros.
/// </summary>
internal static class TraductorDeErroresSqlServer
{
    private const int ErrorDeInicioDeSesion = 18456;
    private const int ErrorDeBaseDeDatosInaccesible = 4060;
    private const int ErrorDeRedGeneral = -1;
    private const int ErrorDeServidorNoEncontrado = 2;
    private const int ErrorDeRedNoEncontrada = 53;
    private const int ErrorDeTiempoDeEspera = -2;

    public static ErrorDeConexion TraducirErrorDeConexion(SqlException excepcion)
    {
        var mensaje = excepcion.Number switch
        {
            ErrorDeInicioDeSesion =>
                "No se pudo iniciar sesión. Revisa el usuario y la contraseña.",
            ErrorDeBaseDeDatosInaccesible =>
                "La base de datos no existe o el usuario no tiene permiso para abrirla.",
            ErrorDeRedGeneral or ErrorDeServidorNoEncontrado or ErrorDeRedNoEncontrada =>
                "No se encontró el servidor. Verifica que SQL Server esté en ejecución y que el nombre y puerto sean correctos.",
            ErrorDeTiempoDeEspera =>
                "El servidor tardó demasiado en responder.",
            _ => excepcion.Message
        };

        return new ErrorDeConexion(mensaje, excepcion);
    }
}
