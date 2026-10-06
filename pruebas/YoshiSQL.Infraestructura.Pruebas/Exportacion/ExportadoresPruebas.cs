using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Infraestructura.Exportacion;

namespace YoshiSQL.Infraestructura.Pruebas.Exportacion;

public class ExportadoresPruebas
{
    private static readonly ConjuntoDeResultados ConjuntoDePrueba = new(
        [new ColumnaDeResultado("Id", "int"), new ColumnaDeResultado("Nombre", "nvarchar"), new ColumnaDeResultado("Activo", "bit")],
        [
            [1, "Peña, José", true],
            [2, "Dice \"hola\"\ny adiós", null]
        ]);

    [Fact]
    public async Task ExportadorCsv_EscapaComasComillasYSaltosDeLinea()
    {
        var texto = await ExportarComoTextoAsync(new ExportadorCsv());

        Assert.StartsWith("﻿", texto);
        Assert.Equal(
            "Id,Nombre,Activo\r\n1,\"Peña, José\",1\r\n2,\"Dice \"\"hola\"\"\ny adiós\",\r\n",
            texto.TrimStart('﻿'));
    }

    [Fact]
    public async Task ExportadorJson_ConservaTiposYNulos()
    {
        var texto = await ExportarComoTextoAsync(new ExportadorJson());
        var filas = JsonDocument.Parse(texto).RootElement;

        Assert.Equal(2, filas.GetArrayLength());
        Assert.Equal(1, filas[0].GetProperty("Id").GetInt32());
        Assert.Equal("Peña, José", filas[0].GetProperty("Nombre").GetString());
        Assert.True(filas[0].GetProperty("Activo").GetBoolean());
        Assert.Equal(JsonValueKind.Null, filas[1].GetProperty("Activo").ValueKind);
        Assert.Contains("Peña", texto);
    }

    [Fact]
    public async Task ExportadorExcel_GeneraUnLibroValidoConLosValores()
    {
        using var memoria = new MemoryStream();
        await new ExportadorExcel().ExportarAsync(ConjuntoDePrueba, memoria, CancellationToken.None);

        memoria.Position = 0;
        using var libro = new ZipArchive(memoria, ZipArchiveMode.Read);
        Assert.NotNull(libro.GetEntry("[Content_Types].xml"));
        Assert.NotNull(libro.GetEntry("xl/workbook.xml"));

        using var flujoDeLaHoja = libro.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var hoja = XDocument.Load(flujoDeLaHoja);
        XNamespace principal = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var celdas = hoja.Descendants(principal + "c").ToDictionary(celda => (string)celda.Attribute("r")!, celda => celda.Value);

        Assert.Equal("Nombre", celdas["B1"]);
        Assert.Equal("1", celdas["A2"]);
        Assert.Equal("Peña, José", celdas["B2"]);
        Assert.False(celdas.ContainsKey("C3"));
    }

    [Theory]
    [InlineData(0, "A")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void ObtenerLetrasDeColumna_ConvierteComoExcel(int indice, string esperado)
    {
        Assert.Equal(esperado, ExportadorExcel.ObtenerLetrasDeColumna(indice));
    }

    [Fact]
    public async Task ExportadorDeInsert_GeneraUnInsertPorFilaConLiterales()
    {
        var texto = await ExportarComoTextoAsync(new ExportadorDeInsert());
        var lineas = texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("INSERT INTO [dbo].[TablaDestino] ([Id], [Nombre], [Activo]) VALUES (1, N'Peña, José', 1);", lineas[1]);
        Assert.Equal("INSERT INTO [dbo].[TablaDestino] ([Id], [Nombre], [Activo]) VALUES (2, N'Dice \"hola\"\ny adiós', NULL);", lineas[2]);
    }

    private static async Task<string> ExportarComoTextoAsync(Dominio.Contratos.IExportadorDeResultados exportador)
    {
        using var memoria = new MemoryStream();
        await exportador.ExportarAsync(ConjuntoDePrueba, memoria, CancellationToken.None);
        return new UTF8Encoding(false).GetString(memoria.ToArray());
    }
}
