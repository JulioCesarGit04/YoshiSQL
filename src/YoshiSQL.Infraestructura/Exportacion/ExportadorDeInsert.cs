using System.Globalization;
using System.Text;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;
using static YoshiSQL.Infraestructura.SqlServer.DelimitadorDeIdentificadores;

namespace YoshiSQL.Infraestructura.Exportacion;

/// <summary>
/// Genera una sentencia INSERT por fila. Como un conjunto de resultados no sabe de qué tabla viene,
/// usa un nombre de tabla de ejemplo que el usuario reemplaza por el suyo.
/// </summary>
public sealed class ExportadorDeInsert : IExportadorDeResultados
{
    private const string TablaDeEjemplo = "[dbo].[TablaDestino]";
    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    public FormatoDeExportacion Formato => FormatoDeExportacion.Insert;

    public string Extension => "sql";

    public async Task ExportarAsync(ConjuntoDeResultados conjunto, Stream destino, CancellationToken tokenDeCancelacion)
    {
        await using var escritor = new StreamWriter(destino, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
        escritor.NewLine = "\r\n";

        await escritor.WriteLineAsync($"-- Reemplaza {TablaDeEjemplo} por la tabla de destino.");

        var columnas = string.Join(", ", conjunto.Columnas.Select(columna => Delimitar(columna.Nombre)));

        foreach (var fila in conjunto.Filas)
        {
            tokenDeCancelacion.ThrowIfCancellationRequested();
            var valores = string.Join(", ", fila.Select(EscribirLiteral));
            await escritor.WriteLineAsync($"INSERT INTO {TablaDeEjemplo} ({columnas}) VALUES ({valores});");
        }
    }

    /// <summary>Escribe un valor como literal de T-SQL: números sin comillas, texto y fechas entre comillas, binarios en hexadecimal.</summary>
    private static string EscribirLiteral(object? valor) => valor switch
    {
        null => "NULL",
        bool booleano => booleano ? "1" : "0",
        byte[] bytes => $"0x{Convert.ToHexString(bytes)}",
        DateTime fecha => $"'{fecha.ToString("yyyy-MM-dd HH:mm:ss.fff", Invariante)}'",
        DateTimeOffset fecha => $"'{fecha.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", Invariante)}'",
        TimeSpan hora => $"'{hora.ToString("c", Invariante)}'",
        Guid identificador => $"'{identificador}'",
        byte or short or int or long or decimal or double or float => Convert.ToString(valor, Invariante) ?? "NULL",
        _ => $"N'{valor.ToString()!.Replace("'", "''", StringComparison.Ordinal)}'"
    };
}
