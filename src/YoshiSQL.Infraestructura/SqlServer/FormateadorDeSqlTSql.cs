using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Errores;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Formatea T-SQL con el analizador y el generador oficiales de Microsoft (ScriptDom).
/// </summary>
public sealed partial class FormateadorDeSqlTSql : IFormateadorDeSql
{
    private const int TamanoDeSangria = 4;

    private static readonly SqlScriptGeneratorOptions OpcionesDeFormato = new()
    {
        KeywordCasing = KeywordCasing.Uppercase,
        IncludeSemicolons = true,
        IndentationSize = TamanoDeSangria,
        NewLineBeforeFromClause = true,
        NewLineBeforeWhereClause = true,
        NewLineBeforeJoinClause = true,
        NewLineBeforeGroupByClause = true,
        NewLineBeforeOrderByClause = true,
        NewLineBeforeHavingClause = true,
        AlignClauseBodies = false
    };

    public string Formatear(string textoSql)
    {
        var analizador = new TSql170Parser(initialQuotedIdentifiers: true);
        var arbolDelScript = analizador.Parse(new StringReader(textoSql), out var erroresDeSintaxis);

        if (erroresDeSintaxis.Count > 0)
        {
            var primerError = erroresDeSintaxis[0];
            throw new ErrorDeYoshiSql(
                $"No se puede formatear porque el código tiene un error de sintaxis en la línea {primerError.Line}: {primerError.Message}");
        }

        // El generador de Microsoft descarta los comentarios; se evita borrarlos sin avisar
        if (TieneComentarios(arbolDelScript))
        {
            throw new ErrorDeYoshiSql(
                "No se puede formatear este código porque contiene comentarios y el formateador los eliminaría.\n"
                + "Subraya solo la parte sin comentarios y vuelve a formatear.");
        }

        new Sql170ScriptGenerator(OpcionesDeFormato).GenerateScript(arbolDelScript, out var codigoFormateado);
        return NormalizarSeparadores(codigoFormateado);
    }

    private static bool TieneComentarios(TSqlFragment arbolDelScript) =>
        arbolDelScript.ScriptTokenStream.Any(token =>
            token.TokenType is TSqlTokenType.SingleLineComment or TSqlTokenType.MultilineComment);

    /// <summary>
    /// El generador deja líneas en blanco de más alrededor de cada GO; se dejan en un formato uniforme.
    /// </summary>
    private static string NormalizarSeparadores(string codigo)
    {
        // El generador usa el salto de línea del sistema (\r\n en Windows); las expresiones trabajan con \n
        var codigoConSaltosUnix = codigo.ReplaceLineEndings("\n");
        var codigoSinEspaciosAlFinal = ExpresionDeEspaciosAlFinalDeLinea().Replace(codigoConSaltosUnix, string.Empty);
        var codigoConGoUniforme = ExpresionDeSeparadorGo().Replace(codigoSinEspaciosAlFinal, "\nGO\n\n");
        var codigoNormalizado = ExpresionDeLineasEnBlancoRepetidas().Replace(codigoConGoUniforme, "\n\n").Trim();
        return codigoNormalizado.ReplaceLineEndings(Environment.NewLine);
    }

    [GeneratedRegex(@"[ \t]+$", RegexOptions.Multiline)]
    private static partial Regex ExpresionDeEspaciosAlFinalDeLinea();

    [GeneratedRegex(@"\n\s*\nGO\n\s*", RegexOptions.IgnoreCase)]
    private static partial Regex ExpresionDeSeparadorGo();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExpresionDeLineasEnBlancoRepetidas();
}
