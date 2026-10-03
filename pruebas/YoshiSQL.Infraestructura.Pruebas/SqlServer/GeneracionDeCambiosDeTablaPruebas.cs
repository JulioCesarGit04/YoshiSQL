using YoshiSQL.Dominio.Diseno;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class GeneracionDeCambiosDeTablaPruebas
{
    private static readonly TipoDeDato Entero = new("int");
    private static readonly TipoDeDato Texto100 = new("nvarchar", 100);

    private static readonly DefinicionDeTabla TablaOriginal = new("dbo", "Clientes",
    [
        new("Id", "Id", Entero, AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true),
        new("Nombre", "Nombre", Texto100, AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false),
        new("Ciudad", "Ciudad", new TipoDeDato("nvarchar", 50), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false)
    ], NombreDeLaLlavePrimaria: "PK_Clientes");

    private readonly GeneradorDeScriptsSqlServer _generador = new();

    [Fact]
    public void TablaNueva_GeneraCreateTableEnUnaTransaccion()
    {
        var script = _generador.GenerarCambiosDeTabla("Ventas", original: null, TablaOriginal with { NombreDeLaLlavePrimaria = null });

        Assert.Contains("SET XACT_ABORT ON;", script);
        Assert.Contains("BEGIN TRANSACTION;", script);
        Assert.Contains("CREATE TABLE [dbo].[Clientes]", script);
        Assert.Contains("[Id] int IDENTITY(1,1) NOT NULL", script);
        Assert.Contains("CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])", script);
        Assert.Contains("COMMIT TRANSACTION;", script);
    }

    [Fact]
    public void AgregarRenombrarYEliminarColumnas_GeneraCadaInstruccionEnOrden()
    {
        var nueva = TablaOriginal with
        {
            Columnas =
            [
                TablaOriginal.Columnas[0],
                TablaOriginal.Columnas[1] with { Nombre = "NombreCompleto" },
                new(null, "Correo", Texto100, AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false)
            ]
        };

        var script = _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, nueva);

        var posicionDelDrop = script.IndexOf("DROP COLUMN [Ciudad]", StringComparison.Ordinal);
        var posicionDelRename = script.IndexOf("EXEC sp_rename N'[dbo].[Clientes].[Nombre]', N'NombreCompleto', N'COLUMN';", StringComparison.Ordinal);
        var posicionDelAdd = script.IndexOf("ADD [Correo] nvarchar(100) NULL;", StringComparison.Ordinal);

        Assert.True(posicionDelDrop > 0 && posicionDelRename > posicionDelDrop && posicionDelAdd > posicionDelRename, script);
        Assert.DoesNotContain("PRIMARY KEY", script);
    }

    [Fact]
    public void CambiarTipoDeColumnaDeLaLlave_QuitaYVuelveACrearLaLlave()
    {
        var nueva = TablaOriginal with
        {
            Columnas = [TablaOriginal.Columnas[0] with { TipoDeDato = new TipoDeDato("bigint") }, .. TablaOriginal.Columnas.Skip(1)]
        };

        var script = _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, nueva);

        var posicionDelDropDeLaLlave = script.IndexOf("DROP CONSTRAINT [PK_Clientes]", StringComparison.Ordinal);
        var posicionDelAlter = script.IndexOf("ALTER COLUMN [Id] bigint NOT NULL", StringComparison.Ordinal);
        var posicionDeLaNuevaLlave = script.IndexOf("ADD CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])", StringComparison.Ordinal);

        Assert.True(posicionDelDropDeLaLlave > 0 && posicionDelAlter > posicionDelDropDeLaLlave && posicionDeLaNuevaLlave > posicionDelAlter, script);
    }

    [Fact]
    public void SinCambios_NoGeneraInstrucciones()
    {
        Assert.False(CambiosDeTabla.Calcular(TablaOriginal, TablaOriginal).HayCambios);
        Assert.Contains("No hay cambios", _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, TablaOriginal));
    }
}
