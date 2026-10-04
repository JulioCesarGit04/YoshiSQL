using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Con STATISTICS XML el servidor agrega, después de cada instrucción, un conjunto de resultados
/// con el plan real. Se separan para que la grilla muestre solo los datos.
/// </summary>
internal static class SeparadorDePlanesReales
{
    // Nombre que usa SQL Server para esa columna; se compara además el contenido
    // por si alguna versión lo cambia
    private const string NombreDeColumnaDelPlan = "Microsoft SQL Server 2005 XML Showplan";
    private const string InicioDeUnPlan = "<ShowPlanXML";

    public static ResultadoDeEjecucion Separar(ResultadoDeEjecucion resultado) => resultado with
    {
        ConjuntosDeResultados = resultado.ConjuntosDeResultados.Where(conjunto => !EsConjuntoDePlan(conjunto)).ToList(),
        PlanesRealesXml = resultado.ConjuntosDeResultados
            .Where(EsConjuntoDePlan)
            .SelectMany(conjunto => conjunto.Filas.Select(fila => fila[0] as string))
            .OfType<string>()
            .ToList()
    };

    private static bool EsConjuntoDePlan(ConjuntoDeResultados conjunto) =>
        conjunto.Columnas.Count == 1
        && (conjunto.Columnas[0].Nombre == NombreDeColumnaDelPlan
            || (conjunto.Filas.Count > 0 && conjunto.Filas.All(fila => EsXmlDeUnPlan(fila[0]))));

    private static bool EsXmlDeUnPlan(object? valor) =>
        valor is string texto && texto.TrimStart().StartsWith(InicioDeUnPlan, StringComparison.Ordinal);
}
