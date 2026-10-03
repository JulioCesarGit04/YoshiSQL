using YoshiSQL.Dominio.Conexiones;

namespace YoshiSQL.Escritorio.ModelosDeVista.Conexiones;

public sealed record OpcionDeAutenticacion(TipoDeAutenticacion Tipo, string Descripcion)
{
    public static readonly IReadOnlyList<OpcionDeAutenticacion> Todas =
    [
        new(TipoDeAutenticacion.SqlServer, "Autenticación de SQL Server"),
        new(TipoDeAutenticacion.Windows, "Autenticación integrada (Kerberos)")
    ];
}
