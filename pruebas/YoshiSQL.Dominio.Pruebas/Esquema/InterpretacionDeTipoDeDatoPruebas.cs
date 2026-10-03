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
    public void Interpretar_TextoValido_DevuelveElTipo(string texto, string esperado)
    {
        Assert.Equal(esperado, TipoDeDato.Interpretar(texto)?.Describir());
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
