using System.Globalization;
using System.IO.Compression;
using System.Text;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Diagramas;

namespace YoshiSQL.Infraestructura.Exportacion;

/// <summary>
/// Genera un PDF mínimo de una página con una imagen, sin depender de ninguna biblioteca:
/// el formato solo necesita cinco objetos (catálogo, páginas, página, imagen y la instrucción que la dibuja).
/// </summary>
public sealed class EscritorDePdf : IEscritorDePdf
{
    // El formato PDF no admite páginas de más de 200 pulgadas (14.400 puntos) por lado
    private const double LadoMaximoEnPuntos = 14400;

    private const string Encabezado = "%PDF-1.4\n";

    public async Task EscribirImagenAsync(
        Stream destino,
        ImagenRgb imagen,
        double anchoEnPuntos,
        double altoEnPuntos,
        CancellationToken tokenDeCancelacion)
    {
        if (imagen.Pixeles.Length != imagen.Ancho * imagen.Alto * ImagenRgb.BytesPorPixel)
        {
            throw new ArgumentException("La cantidad de bytes no corresponde al tamaño de la imagen.", nameof(imagen));
        }

        var (ancho, alto) = AjustarAlMaximoDePagina(anchoEnPuntos, altoEnPuntos);
        var pixelesComprimidos = Comprimir(imagen.Pixeles);
        var dibujo = Encoding.ASCII.GetBytes($"q {Numero(ancho)} 0 0 {Numero(alto)} 0 0 cm /Im0 Do Q");

        using var documento = new MemoryStream();
        var posicionDeCadaObjeto = new List<long>();

        Escribir(documento, Encabezado);

        EscribirObjeto(documento, posicionDeCadaObjeto, "<< /Type /Catalog /Pages 2 0 R >>");
        EscribirObjeto(documento, posicionDeCadaObjeto, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        EscribirObjeto(
            documento,
            posicionDeCadaObjeto,
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Numero(ancho)} {Numero(alto)}] "
            + "/Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>");
        EscribirObjetoConDatos(
            documento,
            posicionDeCadaObjeto,
            $"<< /Type /XObject /Subtype /Image /Width {imagen.Ancho} /Height {imagen.Alto} "
            + $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {pixelesComprimidos.Length} >>",
            pixelesComprimidos);
        EscribirObjetoConDatos(documento, posicionDeCadaObjeto, $"<< /Length {dibujo.Length} >>", dibujo);

        EscribirTablaDeReferencias(documento, posicionDeCadaObjeto);

        documento.Position = 0;
        await documento.CopyToAsync(destino, tokenDeCancelacion);
    }

    private static (double Ancho, double Alto) AjustarAlMaximoDePagina(double ancho, double alto)
    {
        var mayor = Math.Max(ancho, alto);

        if (mayor <= LadoMaximoEnPuntos)
        {
            return (ancho, alto);
        }

        var reduccion = LadoMaximoEnPuntos / mayor;
        return (ancho * reduccion, alto * reduccion);
    }

    private static byte[] Comprimir(byte[] datos)
    {
        using var comprimido = new MemoryStream();

        using (var compresor = new ZLibStream(comprimido, CompressionLevel.Optimal, leaveOpen: true))
        {
            compresor.Write(datos, 0, datos.Length);
        }

        return comprimido.ToArray();
    }

    private static void EscribirObjeto(Stream documento, List<long> posiciones, string contenido)
    {
        posiciones.Add(documento.Position);
        Escribir(documento, $"{posiciones.Count} 0 obj\n{contenido}\nendobj\n");
    }

    private static void EscribirObjetoConDatos(Stream documento, List<long> posiciones, string diccionario, byte[] datos)
    {
        posiciones.Add(documento.Position);
        Escribir(documento, $"{posiciones.Count} 0 obj\n{diccionario}\nstream\n");
        documento.Write(datos, 0, datos.Length);
        Escribir(documento, "\nendstream\nendobj\n");
    }

    /// <summary>
    /// La tabla final dice en qué byte empieza cada objeto; los lectores de PDF empiezan a leer por ahí.
    /// Cada entrada mide exactamente 20 bytes: es parte del formato.
    /// </summary>
    private static void EscribirTablaDeReferencias(Stream documento, List<long> posiciones)
    {
        var inicioDeLaTabla = documento.Position;
        var tabla = new StringBuilder($"xref\n0 {posiciones.Count + 1}\n0000000000 65535 f \n");

        foreach (var posicion in posiciones)
        {
            tabla.Append(CultureInfo.InvariantCulture, $"{posicion:D10} 00000 n \n");
        }

        tabla.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {posiciones.Count + 1} /Root 1 0 R >>\nstartxref\n{inicioDeLaTabla}\n%%EOF\n");
        Escribir(documento, tabla.ToString());
    }

    private static void Escribir(Stream documento, string texto)
    {
        var bytes = Encoding.ASCII.GetBytes(texto);
        documento.Write(bytes, 0, bytes.Length);
    }

    // Punto decimal siempre, sea cual sea el idioma del equipo: una coma rompería el archivo
    private static string Numero(double valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);
}
