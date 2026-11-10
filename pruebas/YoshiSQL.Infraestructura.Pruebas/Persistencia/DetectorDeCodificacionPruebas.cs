using System.Text;
using YoshiSQL.Infraestructura.Persistencia;

namespace YoshiSQL.Infraestructura.Pruebas.Persistencia;

public class DetectorDeCodificacionPruebas
{
    private const string Script = "SELECT [Descripción], N'Año' FROM dbo.Niños; -- canción";

    // Así guardan SSMS y el Bloc de notas los archivos "ANSI": un byte por letra, la ñ es 0xF1
    private static readonly byte[] ScriptEnWindows1252 = Encoding.Latin1.GetBytes(Script);

    [Fact]
    public void Decodificar_ArchivoAnsiConTildes_ConservaLasTildes()
    {
        Assert.Equal(Script, DetectorDeCodificacion.Decodificar(ScriptEnWindows1252));
    }

    [Fact]
    public void Decodificar_Utf8SinMarca_ConservaLasTildes()
    {
        Assert.Equal(Script, DetectorDeCodificacion.Decodificar(new UTF8Encoding(false).GetBytes(Script)));
    }

    [Fact]
    public void Decodificar_Utf8ConMarca_QuitaLaMarca()
    {
        byte[] bytes = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(Script)];

        Assert.Equal(Script, DetectorDeCodificacion.Decodificar(bytes));
    }

    [Fact]
    public void Decodificar_Utf16ConMarca_ComoLosScriptsGeneradosPorSsms()
    {
        byte[] bytes = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(Script)];

        Assert.Equal(Script, DetectorDeCodificacion.Decodificar(bytes));
    }

    [Fact]
    public void Decodificar_Utf16SinMarca_LoReconocePorLosCeros()
    {
        Assert.Equal(Script, DetectorDeCodificacion.Decodificar(Encoding.Unicode.GetBytes(Script)));
    }

    [Fact]
    public void Decodificar_ComillasTipograficasDeWindows_NoSonLatin1()
    {
        // 0x93 y 0x94 son comillas tipográficas en Windows-1252 (en Latin-1 serían caracteres de control)
        byte[] bytes = [0x93, (byte)'a', 0x94, 0xF1];

        Assert.Equal("“a”ñ", DetectorDeCodificacion.Decodificar(bytes));
    }

    [Fact]
    public void Decodificar_ArchivoVacio_DevuelveVacio()
    {
        Assert.Equal(string.Empty, DetectorDeCodificacion.Decodificar([]));
    }
}
