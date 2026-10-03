using System.Globalization;
using System.Xml.Linq;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Planes;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Convierte el XML de planes de SQL Server (showplan) en un árbol de operaciones con su costo.
/// </summary>
public sealed class AnalizadorDePlanesSqlServer : IAnalizadorDePlanes
{
    private static readonly XNamespace EspacioDeNombres = "http://schemas.microsoft.com/sqlserver/2004/07/showplan";
    private static readonly XName Instruccion = EspacioDeNombres + "StmtSimple";
    private static readonly XName PlanDeConsulta = EspacioDeNombres + "QueryPlan";
    private static readonly XName Operacion = EspacioDeNombres + "RelOp";
    private static readonly XName Objeto = EspacioDeNombres + "Object";
    private static readonly XName Advertencias = EspacioDeNombres + "Warnings";
    private static readonly XName IndicesFaltantes = EspacioDeNombres + "MissingIndexes";
    private static readonly XName InformacionDeEjecucion = EspacioDeNombres + "RunTimeInformation";

    private static readonly Dictionary<string, string> AdvertenciasEnEspanol = new()
    {
        ["NoJoinPredicate"] = "Unión sin condición: puede multiplicar las filas",
        ["SpillToTempDb"] = "Usó tempdb por falta de memoria",
        ["PlanAffectingConvert"] = "Conversión implícita que empeora el plan",
        ["ColumnsWithNoStatistics"] = "Columnas sin estadísticas",
        ["UnmatchedIndexes"] = "Índices filtrados que no se pudieron usar",
        ["MemoryGrantWarning"] = "Asignación de memoria inadecuada",
        [IndicesFaltantes.LocalName] = "Falta un índice que mejoraría esta consulta"
    };

    public PlanDeEjecucion Interpretar(IReadOnlyList<string> documentosXml, bool esReal)
    {
        var instrucciones = documentosXml
            .SelectMany(xml => XDocument.Parse(xml).Descendants(Instruccion))
            .Select(InterpretarInstruccion)
            .ToList();

        return new PlanDeEjecucion(instrucciones, esReal, documentosXml);
    }

    private static InstruccionDelPlan InterpretarInstruccion(XElement instruccion)
    {
        var costoTotal = LeerNumero(instruccion, "StatementSubTreeCost");
        var planDeConsulta = instruccion.Element(PlanDeConsulta);
        var operacionRaiz = planDeConsulta?.Element(Operacion);

        var advertencias = LeerAdvertencias(planDeConsulta?.Element(Advertencias)).ToList();

        if (planDeConsulta?.Element(IndicesFaltantes) is not null)
        {
            advertencias.Add(AdvertenciasEnEspanol[IndicesFaltantes.LocalName]);
        }

        return new InstruccionDelPlan(
            ((string?)instruccion.Attribute("StatementText") ?? string.Empty).Trim(),
            costoTotal,
            operacionRaiz is null ? null : InterpretarOperacion(operacionRaiz, costoTotal),
            advertencias);
    }

    private static NodoDelPlan InterpretarOperacion(XElement operacion, double costoTotalDeLaInstruccion)
    {
        var operacionesHijas = BuscarDescendientesPropios(operacion, Operacion).ToList();
        var costoDelSubarbol = LeerNumero(operacion, "EstimatedTotalSubtreeCost");
        var costoDeLosHijos = operacionesHijas.Sum(hija => LeerNumero(hija, "EstimatedTotalSubtreeCost"));

        // El costo propio es el del subárbol menos lo que aportan las operaciones hijas
        var costoPropio = Math.Max(0, costoDelSubarbol - costoDeLosHijos);
        var porcentaje = costoTotalDeLaInstruccion > 0 ? costoPropio / costoTotalDeLaInstruccion * 100 : 0;

        var contadoresReales = operacion.Element(InformacionDeEjecucion)?.Elements().ToList();

        return new NodoDelPlan(
            (string?)operacion.Attribute("PhysicalOp") ?? "?",
            (string?)operacion.Attribute("LogicalOp") ?? string.Empty,
            DescribirObjeto(BuscarDescendientesPropios(operacion, Objeto).FirstOrDefault()),
            Math.Round(porcentaje, 1),
            LeerNumero(operacion, "EstimateRows"),
            contadoresReales is { Count: > 0 } ? contadoresReales.Sum(contador => (long?)contador.Attribute("ActualRows") ?? 0) : null,
            contadoresReales is { Count: > 0 } ? contadoresReales.Sum(contador => (long?)contador.Attribute("ActualExecutions") ?? 0) : null,
            LeerAdvertencias(operacion.Element(Advertencias)).ToList(),
            operacionesHijas.Select(hija => InterpretarOperacion(hija, costoTotalDeLaInstruccion)).ToList());
    }

    /// <summary>
    /// Elementos que pertenecen a esta operación y no a una operación hija (las operaciones se anidan a cualquier profundidad).
    /// </summary>
    private static IEnumerable<XElement> BuscarDescendientesPropios(XElement operacion, XName nombre) =>
        operacion.Descendants(nombre).Where(elemento => elemento.Ancestors(Operacion).FirstOrDefault() == operacion);

    private static string? DescribirObjeto(XElement? objeto)
    {
        if (objeto is null)
        {
            return null;
        }

        var tabla = string.Join('.', new[] { (string?)objeto.Attribute("Schema"), (string?)objeto.Attribute("Table") }.OfType<string>());
        var indice = (string?)objeto.Attribute("Index");
        return indice is null ? tabla : $"{tabla} {indice}";
    }

    private static IEnumerable<string> LeerAdvertencias(XElement? advertencias) =>
        advertencias?.Elements().Select(advertencia => AdvertenciasEnEspanol.GetValueOrDefault(advertencia.Name.LocalName, advertencia.Name.LocalName))
        ?? [];

    private static double LeerNumero(XElement elemento, string atributo) =>
        double.TryParse((string?)elemento.Attribute(atributo), NumberStyles.Float, CultureInfo.InvariantCulture, out var valor) ? valor : 0;
}
