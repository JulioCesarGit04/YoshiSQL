using YoshiSQL.Dominio.Diagramas;

namespace YoshiSQL.Dominio.Contratos;

public interface IEscritorDePdf
{
    /// <summary>
    /// Escribe un PDF de una sola página que contiene la imagen ocupando toda la hoja.
    /// </summary>
    /// <param name="anchoEnPuntos">Ancho de la página en puntos tipográficos (72 por pulgada).</param>
    Task EscribirImagenAsync(
        Stream destino,
        ImagenRgb imagen,
        double anchoEnPuntos,
        double altoEnPuntos,
        CancellationToken tokenDeCancelacion);
}
