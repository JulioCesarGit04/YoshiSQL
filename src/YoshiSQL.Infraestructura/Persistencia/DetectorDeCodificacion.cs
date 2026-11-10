using System.Text;

namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Convierte los bytes de un archivo de texto en texto, averiguando su codificación:
/// la marca inicial (BOM) si la tiene; si no, UTF-8 cuando los bytes son UTF-8 válido; y si tampoco,
/// la página de códigos de Windows para Europa occidental (1252), que es como guardan SSMS y el Bloc de notas
/// los archivos "ANSI" en equipos en español.
/// </summary>
public static class DetectorDeCodificacion
{
    private const int PaginaDeCodigosDeWindowsOccidental = 1252;

    // Sin marca inicial, un archivo UTF-16 se reconoce porque casi la mitad de sus bytes son ceros
    // (cada letra del alfabeto latino ocupa dos bytes y uno de ellos es cero)
    private const int BytesParaReconocerUtf16 = 512;
    private const double ProporcionMinimaDeCerosEnUtf16 = 0.3;

    private static readonly UTF8Encoding Utf8Estricto = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static readonly byte[] MarcaUtf8 = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] MarcaUtf16LittleEndian = [0xFF, 0xFE];
    private static readonly byte[] MarcaUtf16BigEndian = [0xFE, 0xFF];

    private static readonly Lazy<Encoding> Windows1252 = new(() =>
    {
        // Las páginas de códigos de Windows no vienen activas en .NET (y en Linux no existen en el sistema)
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(PaginaDeCodigosDeWindowsOccidental);
    });

    public static string Decodificar(byte[] bytes)
    {
        if (EmpiezaCon(bytes, MarcaUtf8))
        {
            return Encoding.UTF8.GetString(bytes, MarcaUtf8.Length, bytes.Length - MarcaUtf8.Length);
        }

        if (EmpiezaCon(bytes, MarcaUtf16LittleEndian))
        {
            return Encoding.Unicode.GetString(bytes, MarcaUtf16LittleEndian.Length, bytes.Length - MarcaUtf16LittleEndian.Length);
        }

        if (EmpiezaCon(bytes, MarcaUtf16BigEndian))
        {
            return Encoding.BigEndianUnicode.GetString(bytes, MarcaUtf16BigEndian.Length, bytes.Length - MarcaUtf16BigEndian.Length);
        }

        if (ObtenerUtf16SinMarca(bytes) is { } utf16)
        {
            return utf16.GetString(bytes);
        }

        try
        {
            return Utf8Estricto.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Windows1252.Value.GetString(bytes);
        }
    }

    private static bool EmpiezaCon(byte[] bytes, byte[] marca) => bytes.AsSpan().StartsWith(marca);

    private static Encoding? ObtenerUtf16SinMarca(byte[] bytes)
    {
        var cantidad = Math.Min(bytes.Length, BytesParaReconocerUtf16);
        cantidad -= cantidad % 2;

        if (cantidad == 0)
        {
            return null;
        }

        var cerosEnPosicionesPares = 0;
        var cerosEnPosicionesImpares = 0;

        for (var posicion = 0; posicion < cantidad; posicion++)
        {
            if (bytes[posicion] != 0)
            {
                continue;
            }

            if (posicion % 2 == 0)
            {
                cerosEnPosicionesPares++;
            }
            else
            {
                cerosEnPosicionesImpares++;
            }
        }

        var minimoDeCeros = cantidad * ProporcionMinimaDeCerosEnUtf16;

        // En little endian el cero es el segundo byte de cada letra; en big endian, el primero
        if (cerosEnPosicionesImpares >= minimoDeCeros && cerosEnPosicionesPares == 0)
        {
            return Encoding.Unicode;
        }

        return cerosEnPosicionesPares >= minimoDeCeros && cerosEnPosicionesImpares == 0 ? Encoding.BigEndianUnicode : null;
    }
}
