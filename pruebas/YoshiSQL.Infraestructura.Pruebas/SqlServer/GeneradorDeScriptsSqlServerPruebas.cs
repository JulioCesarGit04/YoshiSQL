using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class GeneradorDeScriptsSqlServerPruebas
{
    private readonly GeneradorDeScriptsSqlServer _generador = new();

    [Fact]
    public void GenerarSeleccionDeFilas_NombreConCorchete_LoEscapa()
    {
        var script = _generador.GenerarSeleccionDeFilas("Ventas", new Tabla("dbo", "Mi]Tabla"), 1000);

        Assert.Contains("SELECT TOP (1000) *", script);
        Assert.Contains("FROM [dbo].[Mi]]Tabla];", script);
        Assert.Contains("USE [Ventas];", script);
    }

    [Fact]
    public void GenerarCreacionDeTabla_ConIdentidadYLlavePrimaria_GeneraDefinicionCompleta()
    {
        var columnas = new[]
        {
            new Columna("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true, Posicion: 1),
            new Columna("Nombre", new TipoDeDato("nvarchar", 100), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2)
        };

        var script = _generador.GenerarCreacionDeTabla("Ventas", new Tabla("dbo", "Clientes"), columnas);

        Assert.Contains("CREATE TABLE [dbo].[Clientes]", script);
        Assert.Contains("[Id] int IDENTITY(1,1) NOT NULL", script);
        Assert.Contains("[Nombre] nvarchar(100) NULL", script);
        Assert.Contains("CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])", script);
    }

    [Fact]
    public void GenerarEliminacion_Vista_UsaDropView()
    {
        var script = _generador.GenerarEliminacion("Ventas", new Vista("dbo", "VentasDelMes"));

        Assert.Contains("DROP VIEW IF EXISTS [dbo].[VentasDelMes];", script);
    }
}
