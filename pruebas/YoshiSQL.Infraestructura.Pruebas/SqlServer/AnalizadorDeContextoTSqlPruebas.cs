using YoshiSQL.Dominio.Autocompletado;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class AnalizadorDeContextoTSqlPruebas
{
    private const char MarcaDelCursor = '|';
    private readonly AnalizadorDeContextoTSql _analizador = new();

    [Fact]
    public void Analizar_DespuesDeFrom_EsperaUnaTabla()
    {
        var contexto = Analizar("SELECT * FROM Cli|");

        Assert.Equal(TipoDeContexto.NombreDeTabla, contexto.Tipo);
        Assert.Equal("Cli", contexto.PalabraParcial);
    }

    [Theory]
    [InlineData("UPDATE Ped|")]
    [InlineData("INSERT INTO Ped|")]
    [InlineData("SELECT * FROM dbo.Clientes c INNER JOIN Ped|")]
    public void Analizar_DespuesDeUpdateIntoOJoin_EsperaUnaTabla(string textoConCursor)
    {
        Assert.Equal(TipoDeContexto.NombreDeTabla, Analizar(textoConCursor).Tipo);
    }

    [Fact]
    public void Analizar_DespuesDeAliasConPunto_ReconoceElAliasAunqueElFromEsteDespues()
    {
        var contexto = Analizar("SELECT c.Nom| FROM dbo.Clientes AS c");

        Assert.Equal(TipoDeContexto.DespuesDePunto, contexto.Tipo);
        Assert.Equal("c", contexto.Calificador);
        Assert.Equal("Nom", contexto.PalabraParcial);
        Assert.Equal(new ReferenciaDeTabla("dbo", "Clientes", "c"), Assert.Single(contexto.TablasDeLaInstruccion));
    }

    [Fact]
    public void Analizar_VariasTablasConJoinYComas_LasEncuentraTodas()
    {
        var contexto = Analizar("SELECT | FROM dbo.Clientes c INNER JOIN [dbo].[Pedidos] p ON c.Id = p.ClienteId, Productos");

        // El orden no importa: solo se usan para resolver alias y sugerir columnas
        Assert.Equal(
            [new ReferenciaDeTabla("dbo", "Clientes", "c"), new ReferenciaDeTabla("dbo", "Pedidos", "p"), new ReferenciaDeTabla(null, "Productos", null)],
            contexto.TablasDeLaInstruccion.OrderBy(tabla => tabla.Nombre));
    }

    [Theory]
    [InlineData("SELECT 'hola mun|do'")]
    [InlineData("SELECT 1 -- comentario|")]
    [InlineData("/* bloque | */ SELECT 1")]
    [InlineData("SELECT 'sin cerrar|")]
    public void Analizar_DentroDeTextoOComentario_NoSugiere(string textoConCursor)
    {
        Assert.Equal(TipoDeContexto.SinSugerencias, Analizar(textoConCursor).Tipo);
    }

    [Fact]
    public void Analizar_SoloConsideraLasTablasDelLoteActual()
    {
        var contexto = Analizar("SELECT * FROM Viejo v\nGO\nSELECT | FROM Nuevo n");

        Assert.Equal("Nuevo", Assert.Single(contexto.TablasDeLaInstruccion).Nombre);
    }

    [Fact]
    public void Analizar_PalabraSuelta_EsContextoGeneral()
    {
        var contexto = Analizar("SELECT No|");

        Assert.Equal(TipoDeContexto.General, contexto.Tipo);
        Assert.Equal("No", contexto.PalabraParcial);
    }

    private ContextoDeAutocompletado Analizar(string textoConCursor)
    {
        var posicionDelCursor = textoConCursor.IndexOf(MarcaDelCursor);
        return _analizador.Analizar(textoConCursor.Remove(posicionDelCursor, 1), posicionDelCursor);
    }
}

public class AnalizadorDeContextoConSaltosDeWindowsPruebas
{
    [Fact]
    public void Analizar_SaltosDeLineaDeWindows_ReconoceAliasYLote()
    {
        const string texto = "SELECT * FROM Viejo v\r\nGO\r\nSELECT c.\r\nFROM dbo.Clientes c";
        var posicionDelCursor = texto.IndexOf("c.", StringComparison.Ordinal) + 2;

        var contexto = new AnalizadorDeContextoTSql().Analizar(texto, posicionDelCursor);

        Assert.Equal(Dominio.Autocompletado.TipoDeContexto.DespuesDePunto, contexto.Tipo);
        Assert.Equal("Clientes", Assert.Single(contexto.TablasDeLaInstruccion).Nombre);
    }
}
