using System.Text.RegularExpressions;

namespace YoshiSQL.Aplicacion.Consultas;

/// <summary>
/// Reconoce scripts que cambian la lista de bases de datos, para actualizar el explorador automáticamente.
/// </summary>
/// <remarks>
/// Se ejecuta en el hilo de la interfaz después de cada consulta, así que las expresiones deben ser lineales:
/// la versión anterior ("SELECT ... INTO" con un comodín perezoso) recorría el resto del script desde cada SELECT
/// y congelaba la aplicación más de un minuto con scripts de unos cientos de KB.
/// </remarks>
public static partial class DetectorDeCambiosDeEsquema
{
    // Si aun así una expresión tardara demasiado, se corta y se asume que hubo cambios:
    // a lo sumo se vuelve a leer el catálogo del autocompletado, que es inofensivo
    private const int MilisegundosMaximosDeBusqueda = 2000;

    public static bool ModificaBasesDeDatos(string textoSql) => Buscar(ExpresionDeCambioDeBaseDeDatos(), textoSql);

    /// <summary>
    /// Reconoce scripts que crean, modifican, renombran o eliminan tablas o vistas.
    /// </summary>
    public static bool ModificaTablasOVistas(string textoSql) => Buscar(ExpresionDeCambioDeTablasOVistas(), textoSql);

    private static bool Buscar(Regex expresion, string textoSql)
    {
        try
        {
            return expresion.IsMatch(textoSql);
        }
        catch (RegexMatchTimeoutException)
        {
            return true;
        }
    }

    [GeneratedRegex(@"\b(CREATE|DROP|ALTER)\s+DATABASE\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MilisegundosMaximosDeBusqueda)]
    private static partial Regex ExpresionDeCambioDeBaseDeDatos();

    // "SELECT ... INTO tabla" crea una tabla; se reconoce por un INTO que no sea de INSERT ni de MERGE
    // y que no vaya a una variable (@tabla)
    [GeneratedRegex(
        @"\b((CREATE|ALTER|DROP)\s+(TABLE|VIEW)\b|sp_rename\b|(?<!\b(INSERT|MERGE)\s+)\bINTO\s+(?!@))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        MilisegundosMaximosDeBusqueda)]
    private static partial Regex ExpresionDeCambioDeTablasOVistas();
}
