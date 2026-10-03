using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Diagramas;

public sealed record Diagrama(
    string BaseDeDatos,
    IReadOnlyList<NodoDeTabla> Nodos,
    IReadOnlyList<RelacionEntreTablas> Relaciones)
{
    /// <summary>
    /// Indica si la columna de la tabla participa como origen de alguna llave foránea.
    /// </summary>
    public bool EsColumnaForanea(Tabla tabla, string nombreDeColumna) =>
        Relaciones.Any(relacion =>
            relacion.TablaOrigen == tabla
            && relacion.LlaveForanea.ColumnasOrigen.Contains(nombreDeColumna, StringComparer.OrdinalIgnoreCase));
}
