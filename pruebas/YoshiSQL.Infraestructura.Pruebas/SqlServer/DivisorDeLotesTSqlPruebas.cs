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

    [Theory]
    [InlineData("SELECT `columna`;")]
    [InlineData("SELECT ?;")]
    [InlineData("SELECT 'it''s', [col]]x], N'año';")]
    public void DividirEnLotes_ProcedimientosConUnErrorLexico_SeparaCadaProcedimiento(string cuerpoConError)
    {
        var script = $"CREATE PROCEDURE dbo.A AS {cuerpoConError}\nGO\nCREATE PROCEDURE dbo.B AS SELECT 2;\nGO\nCREATE PROCEDURE dbo.C AS SELECT 3;\nGO\n";

        var lotes = _divisor.DividirEnLotes(script);

        Assert.Equal(3, lotes.Count);
        Assert.All(lotes, lote => Assert.StartsWith("CREATE PROCEDURE", lote.Texto, StringComparison.Ordinal));
    }

    [Fact]
    public void DividirEnLotes_ComentarioSinCerrarAlFinal_SeparaLosLotesAnteriores()
    {
        var lotes = _divisor.DividirEnLotes("SELECT 1;\nGO\nSELECT 2;\n/* pendiente\nGO\n");

        Assert.Equal(2, lotes.Count);
    }

    [Fact]
    public void DividirEnLotes_ComentariosAnidadosConGo_NoSepara()
    {
        var lotes = _divisor.DividirEnLotes("/* a /* b */\nGO\n*/\nSELECT 1;");

        Assert.Single(lotes);
    }

    [Fact]
    public void DividirEnLotes_GoDentroDeIdentificadorConEscapes_NoSepara()
    {
        var lotes = _divisor.DividirEnLotes("SELECT 1 AS [a]]\nGO\nb], 'it''s\nGO\n' AS x;");

        Assert.Single(lotes);
    }

    [Theory]
    [InlineData("SELECT 1;\n   go   \nSELECT 2;")]
    [InlineData("SELECT 1;\nGO;\nSELECT 2;")]
    [InlineData("SELECT 1;\nGO--fin\nSELECT 2;")]
    public void DividirEnLotes_VariantesDeGo_Separa(string script)
    {
        Assert.Equal(2, _divisor.DividirEnLotes(script).Count);
    }

    [Theory]
    [InlineData("SELECT 1; GO\nSELECT 2;")]
    [InlineData("GOTO Fin;\nFin: SELECT 1;")]
    public void DividirEnLotes_GoQueNoEstaSoloEnSuLinea_NoSepara(string script)
    {
        Assert.Single(_divisor.DividirEnLotes(script));
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

    [Fact]
    public void DividirEnLotes_SaltosDeLineaSoloRetornoDeCarro_SeparaSinFallar()
    {
        var lotes = new DivisorDeLotesTSql().DividirEnLotes("SELECT 1;\rGO\rSELECT 2;\rGO\r");

        Assert.Equal(2, lotes.Count);
        Assert.Equal([1, 3], lotes.Select(lote => lote.LineaInicial));
    }
}
