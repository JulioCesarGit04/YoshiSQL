using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class SeparadorDePlanesRealesPruebas
{
    private const string Plan = """<ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan" Version="1.599" />""";

    private static readonly ConjuntoDeResultados Datos = new(
        [new ColumnaDeResultado("Id", "int"), new ColumnaDeResultado("Total", "decimal")],
        [[1, 10m]]);

    [Theory]
    [InlineData("Microsoft SQL Server 2005 XML Showplan")]
    [InlineData("Un nombre distinto en otra versión")]
    public void Separar_ReconoceElPlanPorNombreOPorContenido(string nombreDeLaColumna)
    {
        var conjuntoDelPlan = new ConjuntoDeResultados([new ColumnaDeResultado(nombreDeLaColumna, "xml")], [[Plan]]);
        var resultado = CrearResultado(Datos, conjuntoDelPlan);

        var separado = SeparadorDePlanesReales.Separar(resultado);

        Assert.Equal(Datos, Assert.Single(separado.ConjuntosDeResultados));
        Assert.Equal(Plan, Assert.Single(separado.PlanesRealesXml));
    }

    [Fact]
    public void Separar_UnaColumnaDeTextoNormal_NoLaConfundeConUnPlan()
    {
        var textos = new ConjuntoDeResultados([new ColumnaDeResultado("Nombre", "nvarchar")], [["Mario"], ["<ShowPlanXML"]]);

        var separado = SeparadorDePlanesReales.Separar(CrearResultado(textos));

        Assert.Single(separado.ConjuntosDeResultados);
        Assert.Empty(separado.PlanesRealesXml);
    }

    private static ResultadoDeEjecucion CrearResultado(params ConjuntoDeResultados[] conjuntos) =>
        new(EstadoDeEjecucion.Completada, conjuntos, [], TimeSpan.Zero, "PruebaPlanes");
}
