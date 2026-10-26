using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class ConvertidorDeValoresDeEdicionPruebas
{
    private static readonly Columna Saldo =
        new("Saldo", new TipoDeDato("decimal", Precision: 18, Escala: 2), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 1);

    private static readonly Columna Fecha =
        new("Fecha", new TipoDeDato("datetime2"), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2);

    [Theory]
    [InlineData("10.5", 10.5)]
    [InlineData(" -1234.56 ", -1234.56)]
    public void Convertir_DecimalConPunto_LoLee(string texto, double esperado)
    {
        Assert.Equal((decimal)esperado, ConvertidorDeValoresDeEdicion.Convertir(texto, Saldo));
    }

    [Theory]
    [InlineData("10,5")]
    [InlineData("1,234.56")]
    public void Convertir_DecimalConComa_LoRechazaEnVezDeGuardarOtroNumero(string texto)
    {
        var error = Assert.Throws<ErrorDeYoshiSql>(() => ConvertidorDeValoresDeEdicion.Convertir(texto, Saldo));

        Assert.Contains("punto", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Convertir_FechaIso_LaLee()
    {
        Assert.Equal(new DateTime(2026, 1, 5, 13, 45, 0), ConvertidorDeValoresDeEdicion.Convertir("2026-01-05 13:45", Fecha));
    }

    [Fact]
    public void Convertir_FechaConBarras_LaRechazaPorSerAmbigua()
    {
        var error = Assert.Throws<ErrorDeYoshiSql>(() => ConvertidorDeValoresDeEdicion.Convertir("05/01/2026", Fecha));

        Assert.Contains("año-mes-día", error.Message, StringComparison.Ordinal);
    }
}
