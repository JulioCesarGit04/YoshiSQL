using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Consultas;

public sealed class ServicioDeExportacion
{
    private readonly IReadOnlyDictionary<FormatoDeExportacion, IExportadorDeResultados> _exportadoresPorFormato;

    public ServicioDeExportacion(IEnumerable<IExportadorDeResultados> exportadores)
    {
        _exportadoresPorFormato = exportadores.ToDictionary(exportador => exportador.Formato);
    }

    public string ObtenerExtension(FormatoDeExportacion formato) => ObtenerExportador(formato).Extension;

    public async Task ExportarAsync(
        ConjuntoDeResultados conjunto,
        FormatoDeExportacion formato,
        string rutaDelArchivo,
        CancellationToken tokenDeCancelacion)
    {
        await using var archivo = File.Create(rutaDelArchivo);
        await ObtenerExportador(formato).ExportarAsync(conjunto, archivo, tokenDeCancelacion);
    }

    /// <summary>
    /// Texto con columnas separadas por tabulaciones, listo para pegar en una hoja de cálculo.
    /// </summary>
    public async Task<string> CrearTextoParaCopiarAsync(ConjuntoDeResultados conjunto, CancellationToken tokenDeCancelacion)
    {
        using var memoria = new MemoryStream();
        await ObtenerExportador(FormatoDeExportacion.TextoTabulado).ExportarAsync(conjunto, memoria, tokenDeCancelacion);
        return System.Text.Encoding.UTF8.GetString(memoria.ToArray());
    }

    private IExportadorDeResultados ObtenerExportador(FormatoDeExportacion formato) =>
        _exportadoresPorFormato.TryGetValue(formato, out var exportador)
            ? exportador
            : throw new InvalidOperationException($"No hay un exportador registrado para el formato {formato}.");
}
