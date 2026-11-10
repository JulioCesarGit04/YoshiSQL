using System.Text;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Exportacion;

/// <summary>
/// CSV según RFC 4180, en UTF-8 con BOM para que Excel reconozca las tildes.
/// Los valores nulos se escriben como celdas vacías.
/// </summary>
public sealed class ExportadorCsv : IExportadorDeResultados
{
    private const char Separador = ',';
    private const char Comilla = '"';
    private const char Apostrofo = '\'';
    private static readonly Encoding CodificacionConBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    // Excel y LibreOffice ejecutan como fórmula cualquier celda que empiece con uno de estos caracteres
    private static readonly char[] IniciosDeFormula = ['=', '+', '-', '@', '\t', '\r'];

    public FormatoDeExportacion Formato => FormatoDeExportacion.Csv;

    public string Extension => "csv";

    public async Task ExportarAsync(ConjuntoDeResultados conjunto, Stream destino, CancellationToken tokenDeCancelacion)
    {
        await using var escritor = new StreamWriter(destino, CodificacionConBom, leaveOpen: true);
        escritor.NewLine = "\r\n";

        await escritor.WriteLineAsync(UnirCampos(conjunto.Columnas.Select(columna => columna.Nombre)));

        foreach (var fila in conjunto.Filas)
        {
            tokenDeCancelacion.ThrowIfCancellationRequested();
            await escritor.WriteLineAsync(UnirCampos(fila.Select(ConvertirCelda)));
        }
    }

    /// <summary>
    /// Un texto de la base como "=HYPERLINK(...)" se ejecutaría al abrir el CSV en una hoja de cálculo.
    /// Se le antepone un apóstrofo, que la hoja de cálculo muestra como texto. Solo a los textos:
    /// los números negativos (-5) se dejan como están.
    /// </summary>
    private static string? ConvertirCelda(object? valor)
    {
        var texto = ConvertidorDeValoresATexto.Convertir(valor);

        return valor is string && !string.IsNullOrEmpty(texto) && IniciosDeFormula.Contains(texto[0])
            ? Apostrofo + texto
            : texto;
    }

    private static string UnirCampos(IEnumerable<string?> campos) =>
        string.Join(Separador, campos.Select(EscaparCampo));

    /// <summary>
    /// Un campo con coma, comillas o saltos de línea va entre comillas, duplicando las comillas internas.
    /// </summary>
    private static string EscaparCampo(string? campo)
    {
        if (string.IsNullOrEmpty(campo))
        {
            return string.Empty;
        }

        var necesitaComillas = campo.IndexOfAny([Separador, Comilla, '\n', '\r']) >= 0;
        return necesitaComillas ? $"{Comilla}{campo.Replace("\"", "\"\"", StringComparison.Ordinal)}{Comilla}" : campo;
    }
}
