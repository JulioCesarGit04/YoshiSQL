using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Exportacion;

/// <summary>
/// Genera un libro de Excel (.xlsx) sin librerías externas. Un .xlsx es un ZIP con archivos XML
/// (formato Office Open XML); aquí se escribe la versión mínima: una hoja con encabezados en negrita.
/// </summary>
public sealed class ExportadorExcel : IExportadorDeResultados
{
    private const string EspacioDeNombresDeLaHoja = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const int LargoMaximoDeUnaCelda = 32767;
    private const int EstiloNormal = 0;
    private const int EstiloDeEncabezado = 1;

    public FormatoDeExportacion Formato => FormatoDeExportacion.Excel;

    public string Extension => "xlsx";

    public async Task ExportarAsync(ConjuntoDeResultados conjunto, Stream destino, CancellationToken tokenDeCancelacion)
    {
        await using var libro = new ZipArchive(destino, ZipArchiveMode.Create, leaveOpen: true);

        await EscribirParteAsync(libro, "[Content_Types].xml", TiposDeContenido);
        await EscribirParteAsync(libro, "_rels/.rels", RelacionesDelPaquete);
        await EscribirParteAsync(libro, "xl/workbook.xml", LibroDeTrabajo);
        await EscribirParteAsync(libro, "xl/_rels/workbook.xml.rels", RelacionesDelLibro);
        await EscribirParteAsync(libro, "xl/styles.xml", Estilos);

        var entradaDeLaHoja = libro.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Optimal);
        await using var flujoDeLaHoja = await entradaDeLaHoja.OpenAsync(tokenDeCancelacion);
        EscribirHoja(conjunto, flujoDeLaHoja, tokenDeCancelacion);
    }

    private static void EscribirHoja(ConjuntoDeResultados conjunto, Stream destino, CancellationToken tokenDeCancelacion)
    {
        using var escritor = XmlWriter.Create(destino, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });

        escritor.WriteStartDocument(standalone: true);
        escritor.WriteStartElement("worksheet", EspacioDeNombresDeLaHoja);
        escritor.WriteStartElement("sheetData", EspacioDeNombresDeLaHoja);

        EscribirFila(escritor, numeroDeFila: 1, conjunto.Columnas.Select(columna => (object?)columna.Nombre).ToArray(), EstiloDeEncabezado);

        for (var indice = 0; indice < conjunto.Filas.Count; indice++)
        {
            tokenDeCancelacion.ThrowIfCancellationRequested();
            EscribirFila(escritor, numeroDeFila: indice + 2, conjunto.Filas[indice], EstiloNormal);
        }

        escritor.WriteEndElement();
        escritor.WriteEndElement();
        escritor.WriteEndDocument();
    }

    private static void EscribirFila(XmlWriter escritor, int numeroDeFila, object?[] valores, int estilo)
    {
        escritor.WriteStartElement("row", EspacioDeNombresDeLaHoja);
        escritor.WriteAttributeString("r", numeroDeFila.ToString(CultureInfo.InvariantCulture));

        for (var columna = 0; columna < valores.Length; columna++)
        {
            // Las celdas nulas simplemente no se escriben
            if (valores[columna] is not { } valor)
            {
                continue;
            }

            escritor.WriteStartElement("c", EspacioDeNombresDeLaHoja);
            escritor.WriteAttributeString("r", $"{ObtenerLetrasDeColumna(columna)}{numeroDeFila}");

            if (estilo != EstiloNormal)
            {
                escritor.WriteAttributeString("s", estilo.ToString(CultureInfo.InvariantCulture));
            }

            EscribirContenidoDeCelda(escritor, valor);
            escritor.WriteEndElement();
        }

        escritor.WriteEndElement();
    }

    private static void EscribirContenidoDeCelda(XmlWriter escritor, object valor)
    {
        if (ConvertidorDeValoresATexto.EsNumero(valor))
        {
            escritor.WriteElementString("v", EspacioDeNombresDeLaHoja, ConvertidorDeValoresATexto.Convertir(valor));
            return;
        }

        if (valor is bool booleano)
        {
            escritor.WriteAttributeString("t", "b");
            escritor.WriteElementString("v", EspacioDeNombresDeLaHoja, booleano ? "1" : "0");
            return;
        }

        escritor.WriteAttributeString("t", "inlineStr");
        escritor.WriteStartElement("is", EspacioDeNombresDeLaHoja);
        escritor.WriteStartElement("t", EspacioDeNombresDeLaHoja);
        escritor.WriteAttributeString("xml", "space", null, "preserve");
        escritor.WriteString(PrepararTexto(ConvertidorDeValoresATexto.Convertir(valor) ?? string.Empty));
        escritor.WriteEndElement();
        escritor.WriteEndElement();
    }

    /// <summary>
    /// Quita los caracteres que XML no admite y recorta al máximo que acepta una celda de Excel.
    /// </summary>
    private static string PrepararTexto(string texto)
    {
        var textoValido = new string(texto.Where(XmlConvert.IsXmlChar).ToArray());
        return textoValido.Length > LargoMaximoDeUnaCelda ? textoValido[..LargoMaximoDeUnaCelda] : textoValido;
    }

    /// <summary>0 -> A, 25 -> Z, 26 -> AA...</summary>
    internal static string ObtenerLetrasDeColumna(int indiceDeColumna)
    {
        var letras = new StringBuilder();

        for (var numero = indiceDeColumna + 1; numero > 0; numero = (numero - 1) / 26)
        {
            letras.Insert(0, (char)('A' + ((numero - 1) % 26)));
        }

        return letras.ToString();
    }

    private static async Task EscribirParteAsync(ZipArchive libro, string ruta, string contenido)
    {
        var entrada = libro.CreateEntry(ruta, CompressionLevel.Optimal);
        await using var flujo = await entrada.OpenAsync();
        await using var escritor = new StreamWriter(flujo, new UTF8Encoding(false));
        await escritor.WriteAsync(contenido);
    }

    private const string TiposDeContenido = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private const string RelacionesDelPaquete = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string LibroDeTrabajo = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets><sheet name="Resultados" sheetId="1" r:id="rId1"/></sheets>
        </workbook>
        """;

    private const string RelacionesDelLibro = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    // Estilo 0: normal. Estilo 1: negrita (encabezados)
    private const string Estilos = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts>
          <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
          <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>
        </styleSheet>
        """;
}
