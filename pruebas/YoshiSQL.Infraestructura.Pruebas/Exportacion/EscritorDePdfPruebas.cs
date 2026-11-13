using System.IO.Compression;
using System.Text;
using YoshiSQL.Dominio.Diagramas;
using YoshiSQL.Infraestructura.Exportacion;

namespace YoshiSQL.Infraestructura.Pruebas.Exportacion;

public class EscritorDePdfPruebas
{
    // 2 x 2 puntos: rojo, verde, azul y blanco
    private static readonly ImagenRgb Imagen = new(2, 2, [255, 0, 0, 0, 255, 0, 0, 0, 255, 255, 255, 255]);

    private readonly EscritorDePdf _escritor = new();

    [Fact]
    public async Task EscribirImagen_GeneraUnPdfConLaPaginaDelTamanoPedido()
    {
        var texto = Encoding.Latin1.GetString(await EscribirAsync(Imagen, 300.5, 200));

        Assert.StartsWith("%PDF-1.4", texto);
        Assert.EndsWith("%%EOF\n", texto);
        Assert.Contains("/MediaBox [0 0 300.5 200]", texto);
        Assert.Contains("/Width 2 /Height 2", texto);
        Assert.Contains("q 300.5 0 0 200 0 0 cm /Im0 Do Q", texto);
    }

    // Los lectores de PDF empiezan por la tabla del final: si una posición está mal, el archivo no abre
    [Fact]
    public async Task EscribirImagen_LaTablaFinalApuntaAlInicioDeCadaObjeto()
    {
        var bytes = await EscribirAsync(Imagen, 100, 100);
        var texto = Encoding.Latin1.GetString(bytes);

        var inicioDeLaTabla = int.Parse(texto[(texto.LastIndexOf("startxref\n", StringComparison.Ordinal) + "startxref\n".Length)..].Split('\n')[0]);
        Assert.StartsWith("xref\n0 6\n", texto[inicioDeLaTabla..]);

        var entradas = texto[inicioDeLaTabla..].Split('\n').Skip(3).Take(5).ToList();

        for (var numero = 1; numero <= entradas.Count; numero++)
        {
            var posicion = int.Parse(entradas[numero - 1][..10]);
            Assert.StartsWith($"{numero} 0 obj\n", texto[posicion..]);
        }
    }

    [Fact]
    public async Task EscribirImagen_LosPuntosSeRecuperanAlDescomprimir()
    {
        var bytes = await EscribirAsync(Imagen, 100, 100);
        var texto = Encoding.Latin1.GetString(bytes);

        var inicio = texto.IndexOf("stream\n", StringComparison.Ordinal) + "stream\n".Length;
        var fin = texto.IndexOf("\nendstream", inicio, StringComparison.Ordinal);

        using var comprimido = new MemoryStream(bytes, inicio, fin - inicio);
        using var descompresor = new ZLibStream(comprimido, CompressionMode.Decompress);
        using var original = new MemoryStream();
        await descompresor.CopyToAsync(original);

        Assert.Equal(Imagen.Pixeles, original.ToArray());
    }

    [Fact]
    public async Task EscribirImagen_PaginaMasGrandeQueElMaximoDelFormato_SeReduceConservandoLaProporcion()
    {
        var texto = Encoding.Latin1.GetString(await EscribirAsync(Imagen, 28800, 14400));

        Assert.Contains("/MediaBox [0 0 14400 7200]", texto);
    }

    [Fact]
    public async Task EscribirImagen_BytesQueNoCorrespondenAlTamano_LoRechaza()
    {
        var imagenIncompleta = new ImagenRgb(2, 2, [255, 0, 0]);

        await Assert.ThrowsAsync<ArgumentException>(() => EscribirAsync(imagenIncompleta, 100, 100));
    }

    private async Task<byte[]> EscribirAsync(ImagenRgb imagen, double ancho, double alto)
    {
        using var destino = new MemoryStream();
        await _escritor.EscribirImagenAsync(destino, imagen, ancho, alto, CancellationToken.None);
        return destino.ToArray();
    }
}
