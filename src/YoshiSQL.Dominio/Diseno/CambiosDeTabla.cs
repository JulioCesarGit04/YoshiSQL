namespace YoshiSQL.Dominio.Diseno;

/// <summary>
/// Diferencias entre una tabla existente y cómo la dejó el usuario en el diseñador.
/// </summary>
public sealed record CambiosDeTabla(
    IReadOnlyList<DefinicionDeColumna> ColumnasNuevas,
    IReadOnlyList<DefinicionDeColumna> ColumnasEliminadas,
    IReadOnlyList<DefinicionDeColumna> ColumnasRenombradas,
    IReadOnlyList<DefinicionDeColumna> ColumnasModificadas,
    bool CambiaLaLlavePrimaria)
{
    public bool HayCambios =>
        ColumnasNuevas.Count > 0
        || ColumnasEliminadas.Count > 0
        || ColumnasRenombradas.Count > 0
        || ColumnasModificadas.Count > 0
        || CambiaLaLlavePrimaria;

    public static CambiosDeTabla Calcular(DefinicionDeTabla original, DefinicionDeTabla nueva)
    {
        var columnasOriginalesPorNombre = original.Columnas.ToDictionary(columna => columna.Nombre, StringComparer.OrdinalIgnoreCase);
        var columnasConservadas = nueva.Columnas.Where(columna => !columna.EsNueva).ToList();

        var columnasEliminadas = original.Columnas
            .Where(columnaOriginal => !columnasConservadas.Any(columna =>
                string.Equals(columna.NombreOriginal, columnaOriginal.Nombre, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var columnasModificadas = columnasConservadas
            .Where(columna => columnasOriginalesPorNombre.TryGetValue(columna.NombreOriginal!, out var columnaOriginal)
                && (columnaOriginal.TipoDeDato != columna.TipoDeDato || columnaOriginal.AdmiteNulos != columna.AdmiteNulos))
            .ToList();

        return new CambiosDeTabla(
            nueva.Columnas.Where(columna => columna.EsNueva).ToList(),
            columnasEliminadas,
            columnasConservadas.Where(columna => columna.FueRenombrada).ToList(),
            columnasModificadas,
            CalcularSiCambiaLaLlavePrimaria(original, nueva, columnasModificadas));
    }

    /// <summary>
    /// La llave cambia si cambian sus columnas o si se modifica alguna de ellas
    /// (SQL Server no permite alterar una columna mientras forma parte de la llave).
    /// </summary>
    private static bool CalcularSiCambiaLaLlavePrimaria(
        DefinicionDeTabla original,
        DefinicionDeTabla nueva,
        IReadOnlyList<DefinicionDeColumna> columnasModificadas)
    {
        var llaveOriginal = original.ColumnasDeLaLlavePrimaria.Select(columna => columna.Nombre);
        var llaveNuevaConNombresOriginales = nueva.ColumnasDeLaLlavePrimaria.Select(columna => columna.NombreOriginal ?? $"(nueva) {columna.Nombre}");

        var cambianLasColumnas = !llaveOriginal.ToHashSet(StringComparer.OrdinalIgnoreCase)
            .SetEquals(llaveNuevaConNombresOriginales);

        var seModificaUnaColumnaDeLaLlave = columnasModificadas.Any(columna =>
            columna.EsLlavePrimaria
            || original.ColumnasDeLaLlavePrimaria.Any(columnaDeLaLlave =>
                string.Equals(columnaDeLaLlave.Nombre, columna.NombreOriginal, StringComparison.OrdinalIgnoreCase)));

        return cambianLasColumnas || seModificaUnaColumnaDeLaLlave;
    }
}
