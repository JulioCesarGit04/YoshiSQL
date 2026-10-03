using Microsoft.Data.SqlClient;
using YoshiSQL.Dominio.Conexiones;

namespace YoshiSQL.Infraestructura.SqlServer;

internal static class ConstructorDeCadenaDeConexion
{
    private const string NombreDeLaAplicacion = "YoshiSQL";
    private const int SegundosDeEsperaParaConectar = 15;

    /// <param name="usarPoolDeConexiones">
    /// Verdadero para consultas cortas del explorador. Falso para las pestañas, que necesitan
    /// su propia conexión física para no compartir estado (USE, #temporales).
    /// </param>
    public static string Construir(DatosDeAcceso datosDeAcceso, string baseDeDatos, bool usarPoolDeConexiones)
    {
        var perfil = datosDeAcceso.Perfil;

        var constructor = new SqlConnectionStringBuilder
        {
            DataSource = perfil.NombreVisible,
            InitialCatalog = baseDeDatos,
            ApplicationName = NombreDeLaAplicacion,
            ConnectTimeout = SegundosDeEsperaParaConectar,
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = perfil.ConfiarEnCertificadoDelServidor,
            Pooling = usarPoolDeConexiones
        };

        if (perfil.TipoDeAutenticacion == TipoDeAutenticacion.Windows)
        {
            constructor.IntegratedSecurity = true;
        }
        else
        {
            constructor.UserID = perfil.Usuario;
            constructor.Password = datosDeAcceso.Contrasena;
        }

        return constructor.ConnectionString;
    }
}
