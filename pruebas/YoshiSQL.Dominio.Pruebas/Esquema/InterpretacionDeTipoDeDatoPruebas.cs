using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Pruebas.Esquema;

public class InterpretacionDeTipoDeDatoPruebas
{
    [Theory]
    [InlineData("int", "int")]
    [InlineData(" NVARCHAR(100) ", "nvarchar(100)")]
    [InlineData("varchar(max)", "varchar(max)")]
    [InlineData("decimal(18, 2)", "decimal(18,2)")]
    [InlineData("datetime2", "datetime2")]
    [InlineData("datetime2(3)", "datetime2(3)")]
    [InlineData("TIME(0)", "time(0)")]
    [InlineData("Telefono", "Telefono")]
    [InlineData("NVARCHAR(MAX)", "nvarchar(max)")]
    public void Interpretar_TextoValido_DevuelveElTipo(string texto, string esperado)
    {
        Assert.Equal(esperado, TipoDeDato.Interpretar(texto)?.Describir());
    }

    // Antes "Name" (tipo de usuario) se comparaba distinto de "name" y cada columna parecía modificada
    [Fact]
    public void Equals_MismoTipoConOtrasMayusculas_SonIguales()
    {
        Assert.Equal(new TipoDeDato("Name"), TipoDeDato.Interpretar("name"));
        Assert.Equal(new TipoDeDato("Name").GetHashCode(), new TipoDeDato("NAME").GetHashCode());
        Assert.NotEqual(new TipoDeDato("datetime2", 3), new TipoDeDato("datetime2", 7));
    }

    [Theory]
    [InlineData("")]
    [InlineData("nvarchar(")]
    [InlineData("nvarchar(0)")]
    [InlineData("decimal(2,5)")]
    [InlineData("int; DROP TABLE x")]
    public void Interpretar_TextoInvalido_DevuelveNulo(string texto)
    {
        Assert.Null(TipoDeDato.Interpretar(texto));
    }
}
