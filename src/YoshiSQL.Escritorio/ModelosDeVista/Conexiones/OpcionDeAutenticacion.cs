using YoshiSQL.Dominio.Conexiones;

namespace YoshiSQL.Escritorio.ModelosDeVista.Conexiones;

public sealed record OpcionDeAutenticacion(TipoDeAutenticacion Tipo, string Descripcion)
{
    public static readonly IReadOnlyList<OpcionDeAutenticacion> Todas =
    [
        new(TipoDeAutenticacion.SqlServer, "Autenticación de SQL Server"),
        // En Windows usa la cuenta con la que iniciaste sesión; en Linux requiere Kerberos configurado
        new(TipoDeAutenticacion.Windows, OperatingSystem.IsWindows() ? "Autenticación de Windows" : "Autenticación integrada (Kerberos)")
    ];
}
