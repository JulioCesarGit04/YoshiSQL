namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Relación donde las columnas de la tabla origen apuntan a las de la tabla destino.
/// </summary>
/// <param name="AlEliminar">Qué pasa con las filas hijas al eliminar la fila padre, como lo nombra el catálogo: NO_ACTION, CASCADE, SET_NULL o SET_DEFAULT.</param>
/// <param name="AlActualizar">Lo mismo cuando cambia la llave de la fila padre.</param>
public sealed record LlaveForanea(
    string Nombre,
    Tabla TablaOrigen,
    IReadOnlyList<string> ColumnasOrigen,
    Tabla TablaDestino,
    IReadOnlyList<string> ColumnasDestino,
    string AlEliminar = LlaveForanea.SinAccion,
    string AlActualizar = LlaveForanea.SinAccion)
{
    public const string SinAccion = "NO_ACTION";
}
