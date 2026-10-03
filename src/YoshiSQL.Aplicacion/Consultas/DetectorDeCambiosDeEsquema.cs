using System.Text.RegularExpressions;

namespace YoshiSQL.Aplicacion.Consultas;

/// <summary>
/// Reconoce scripts que cambian la lista de bases de datos, para actualizar el explorador automáticamente.
/// </summary>
public static partial class DetectorDeCambiosDeEsquema
{
    public static bool ModificaBasesDeDatos(string textoSql) => ExpresionDeCambioDeBaseDeDatos().IsMatch(textoSql);

    [GeneratedRegex(@"\b(CREATE|DROP|ALTER)\s+DATABASE\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpresionDeCambioDeBaseDeDatos();
}
