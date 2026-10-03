using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.Servicios;

public interface IServicioDeExportacionDeResultados
{
    /// <summary>
    /// Pregunta dónde guardar el archivo y exporta el conjunto en el formato indicado.
    /// </summary>
    Task ExportarAsync(ConjuntoDeResultados conjunto, FormatoDeExportacion formato);

    /// <summary>
    /// Copia todas las filas con encabezados, listas para pegar en una hoja de cálculo.
    /// </summary>
    Task CopiarAlPortapapelesAsync(ConjuntoDeResultados conjunto);
}
