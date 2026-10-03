using System.Text;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Exportacion;

/// <summary>
/// Columnas separadas por tabulaciones: es el formato que entienden Excel y LibreOffice al pegar.
/// </summary>
public sealed class ExportadorDeTextoTabulado : IExportadorDeResultados
{
    private const char Separador = '\t';

    public FormatoDeExportacion Formato => FormatoDeExportacion.TextoTabulado;

    public string Extension => "tsv";

    public async Task ExportarAsync(ConjuntoDeResultados conjunto, Stream destino, CancellationToken tokenDeCancelacion)
    {
        await using var escritor = new StreamWriter(destino, new UTF8Encoding(false), leaveOpen: true);

        await escritor.WriteLineAsync(UnirCampos(conjunto.Columnas.Select(columna => columna.Nombre)));

        foreach (var fila in conjunto.Filas)
        {
            tokenDeCancelacion.ThrowIfCancellationRequested();
            await escritor.WriteLineAsync(UnirCampos(fila.Select(valor => ConvertidorDeValoresATexto.Convertir(valor) ?? "NULL")));
        }
    }

    // Tabulaciones y saltos de línea dentro de un valor romperían las columnas al pegar
    private static string UnirCampos(IEnumerable<string> campos) =>
        string.Join(Separador, campos.Select(campo => campo.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ')));
}
