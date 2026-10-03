using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Dominio.Contratos;

/// <summary>
/// Escribe un conjunto de resultados en un formato de archivo. Para agregar un formato
/// nuevo basta con implementar esta interfaz y registrarla.
/// </summary>
public interface IExportadorDeResultados
{
    FormatoDeExportacion Formato { get; }

    /// <summary>Extensión del archivo sin punto, ej. "csv".</summary>
    string Extension { get; }

    Task ExportarAsync(ConjuntoDeResultados conjunto, Stream destino, CancellationToken tokenDeCancelacion);
}
