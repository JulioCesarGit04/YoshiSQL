using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Pruebas.Esquema;

public class TipoDeDatoPruebas
{
    [Theory]
    [InlineData("int", null, null, null, "int")]
    [InlineData("nvarchar", 50, null, null, "nvarchar(50)")]
    [InlineData("nvarchar", -1, null, null, "nvarchar(max)")]
    [InlineData("decimal", null, 18, 2, "decimal(18,2)")]
    public void Describir_SegunLongitudYPrecision_DevuelveFormatoDeSqlServer(
        string nombre, int? longitud, int? precision, int? escala, string esperado)
    {
        var tipoDeDato = new TipoDeDato(nombre, longitud, precision, escala);

        Assert.Equal(esperado, tipoDeDato.Describir());
    }

    [Fact]
    public void DescribirParaExplorador_LlavePrimaria_MuestraComoSsms()
    {
        var columna = new Columna("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true, Posicion: 1);

        Assert.Equal("Id (PK, int, not null)", columna.DescribirParaExplorador());
    }
}
