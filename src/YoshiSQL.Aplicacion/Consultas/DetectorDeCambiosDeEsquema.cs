using System.Text.RegularExpressions;

namespace YoshiSQL.Aplicacion.Consultas;

/// <summary>
/// Reconoce scripts que cambian la lista de bases de datos, para actualizar el explorador automáticamente.
/// </summary>
public static partial class DetectorDeCambiosDeEsquema
{
    public static bool ModificaBasesDeDatos(string textoSql) => ExpresionDeCambioDeBaseDeDatos().IsMatch(textoSql);

    /// <summary>
    /// Reconoce scripts que crean, modifican, renombran o eliminan tablas o vistas.
    /// </summary>
    public static bool ModificaTablasOVistas(string textoSql) => ExpresionDeCambioDeTablasOVistas().IsMatch(textoSql);

    [GeneratedRegex(@"\b(CREATE|DROP|ALTER)\s+DATABASE\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpresionDeCambioDeBaseDeDatos();

    [GeneratedRegex(@"\b((CREATE|ALTER|DROP)\s+(TABLE|VIEW)|sp_rename|SELECT\b[\s\S]*?\bINTO\s+(?!@))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpresionDeCambioDeTablasOVistas();
}
