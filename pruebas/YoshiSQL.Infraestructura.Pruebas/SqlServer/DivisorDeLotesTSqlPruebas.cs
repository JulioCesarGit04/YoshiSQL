using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class DivisorDeLotesTSqlPruebas
{
    private readonly DivisorDeLotesTSql _divisor = new();

    [Fact]
    public void DividirEnLotes_SinGo_DevuelveUnSoloLote()
    {
        var lotes = _divisor.DividirEnLotes("SELECT 1;\nSELECT 2;");

        var lote = Assert.Single(lotes);
        Assert.Equal(1, lote.LineaInicial);
    }

    [Fact]
    public void DividirEnLotes_ConVariosGo_SeparaYCalculaLineas()
    {
        var lotes = _divisor.DividirEnLotes("CREATE DATABASE Prueba;\nGO\nUSE Prueba;\ngo\nSELECT 1;");

        Assert.Equal(3, lotes.Count);
        Assert.Equal("CREATE DATABASE Prueba;\n", lotes[0].Texto);
        Assert.Equal([1, 3, 5], lotes.Select(lote => lote.LineaInicial));
    }

    [Fact]
    public void DividirEnLotes_GoDentroDeTextoComentarioOColumna_NoSepara()
    {
        const string script = "SELECT 'a\nGO\nb' AS go\n/*\nGO\n*/\nSELECT 1;";

        var lotes = _divisor.DividirEnLotes(script);

        Assert.Single(lotes);
    }

    [Fact]
    public void DividirEnLotes_GoConNumero_RegistraRepeticiones()
    {
        var lotes = _divisor.DividirEnLotes("INSERT INTO T DEFAULT VALUES;\nGO 5 -- cinco veces\nSELECT 1;");

        Assert.Equal(5, lotes[0].Repeticiones);
        Assert.Equal(1, lotes[1].Repeticiones);
    }

    [Fact]
    public void DividirEnLotes_SeleccionQueEmpiezaEnLinea10_DesplazaLasLineas()
    {
        var lotes = _divisor.DividirEnLotes("SELECT 1;\nGO\nSELECT 2;", lineaInicialEnElEditor: 10);

        Assert.Equal([10, 12], lotes.Select(lote => lote.LineaInicial));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    [InlineData("GO\nGO\n")]
    public void DividirEnLotes_SinCodigo_NoDevuelveLotes(string script)
    {
        Assert.Empty(_divisor.DividirEnLotes(script));
    }

    [Fact]
    public void DividirEnLotes_ComillaSinCerrar_EnviaTodoEnUnLoteParaQueElServidorInformeElError()
    {
        var lotes = _divisor.DividirEnLotes("SELECT 'sin cerrar\nGO\nSELECT 2;");

        Assert.Single(lotes);
    }
}

public class DivisorDeLotesConSaltosDeWindowsPruebas
{
    [Fact]
    public void DividirEnLotes_SaltosDeLineaDeWindows_SeparaYCalculaLineasIgual()
    {
        var lotes = new DivisorDeLotesTSql().DividirEnLotes("CREATE DATABASE Prueba;\r\nGO\r\nUSE Prueba;\r\nGO 3\r\nSELECT 1;");

        Assert.Equal(3, lotes.Count);
        Assert.Equal([1, 3, 5], lotes.Select(lote => lote.LineaInicial));
        Assert.Equal(3, lotes[1].Repeticiones);
        Assert.DoesNotContain("GO", lotes[1].Texto);
    }
}
