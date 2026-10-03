using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class ProveedorSqlServer : IProveedorDeBaseDeDatos
{
    public ProveedorSqlServer(
        IExploradorDeEsquema explorador,
        IEjecutorDeConsultas ejecutor,
        IDivisorDeLotes divisorDeLotes,
        IGeneradorDeScripts generadorDeScripts,
        IFormateadorDeSql formateador,
        IAnalizadorDeContextoSql analizadorDeContexto)
    {
        Formateador = formateador;
        AnalizadorDeContexto = analizadorDeContexto;
        Explorador = explorador;
        Ejecutor = ejecutor;
        DivisorDeLotes = divisorDeLotes;
        GeneradorDeScripts = generadorDeScripts;
    }

    public string NombreDelMotor => "SQL Server";

    public IExploradorDeEsquema Explorador { get; }

    public IEjecutorDeConsultas Ejecutor { get; }

    public IDivisorDeLotes DivisorDeLotes { get; }

    public IGeneradorDeScripts GeneradorDeScripts { get; }

    public IFormateadorDeSql Formateador { get; }

    public IAnalizadorDeContextoSql AnalizadorDeContexto { get; }

    public IReadOnlyList<string> TiposDeDatoSugeridos { get; } =
    [
        "int", "bigint", "smallint", "tinyint", "bit", "decimal(18,2)", "numeric(18,0)", "money", "float", "real",
        "date", "time", "datetime", "datetime2", "datetimeoffset", "char(10)", "varchar(50)", "varchar(max)",
        "nchar(10)", "nvarchar(50)", "nvarchar(100)", "nvarchar(max)", "varbinary(max)", "uniqueidentifier", "xml"
    ];

    public async Task ProbarConexionAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion)
    {
        var sesion = await Ejecutor.AbrirSesionAsync(
            datosDeAcceso,
            datosDeAcceso.Perfil.BaseDeDatosPredeterminada,
            tokenDeCancelacion);

        await sesion.DisposeAsync();
    }
}
