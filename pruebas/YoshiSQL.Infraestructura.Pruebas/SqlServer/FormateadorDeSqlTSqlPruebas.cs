using YoshiSQL.Dominio.Errores;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class FormateadorDeSqlTSqlPruebas
{
    private readonly FormateadorDeSqlTSql _formateador = new();

    [Fact]
    public void Formatear_ConsultaEnMinusculas_PoneMayusculasYSaltosDeLinea()
    {
        var codigo = _formateador.Formatear("select id,nombre from dbo.clientes where id>5");

        Assert.Equal("SELECT id,\n       nombre\nFROM dbo.clientes\nWHERE id > 5;", codigo.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Formatear_VariosLotes_ConservaLosGo()
    {
        var codigo = _formateador.Formatear("use master\ngo\nselect 1").ReplaceLineEndings("\n");

        Assert.Equal("USE master;\nGO\n\nSELECT 1;", codigo);
    }

    [Fact]
    public void Formatear_CodigoConComentarios_SeNiegaParaNoBorrarlos()
    {
        var error = Assert.Throws<ErrorDeYoshiSql>(() => _formateador.Formatear("-- clientes activos\nselect 1"));

        Assert.Contains("comentarios", error.Message);
    }

    [Fact]
    public void Formatear_ErrorDeSintaxis_IndicaLaLinea()
    {
        var error = Assert.Throws<ErrorDeYoshiSql>(() => _formateador.Formatear("select 1\nselect from where"));

        Assert.Contains("línea 2", error.Message);
    }
}
