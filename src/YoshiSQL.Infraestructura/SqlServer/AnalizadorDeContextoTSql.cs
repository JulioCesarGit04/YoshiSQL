using Microsoft.SqlServer.TransactSql.ScriptDom;
using YoshiSQL.Dominio.Autocompletado;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Determina qué se está escribiendo en la posición del cursor usando el analizador léxico de T-SQL,
/// para no confundir textos, comentarios o palabras clave con nombres de tablas.
/// </summary>
public sealed class AnalizadorDeContextoTSql : IAnalizadorDeContextoSql
{
    private static readonly HashSet<TSqlTokenType> TokensQueIntroducenUnaTabla =
    [
        TSqlTokenType.From,
        TSqlTokenType.Join,
        TSqlTokenType.Update,
        TSqlTokenType.Into
    ];

    private static readonly HashSet<TSqlTokenType> TokensQueTerminanLaClausulaFrom =
    [
        TSqlTokenType.Where,
        TSqlTokenType.Group,
        TSqlTokenType.Order,
        TSqlTokenType.Having,
        TSqlTokenType.Union,
        TSqlTokenType.Except,
        TSqlTokenType.Intersect,
        TSqlTokenType.Option,
        TSqlTokenType.Semicolon,
        TSqlTokenType.Select,
        TSqlTokenType.Insert,
        TSqlTokenType.Update,
        TSqlTokenType.Delete
    ];

    private static readonly HashSet<TSqlTokenType> TokensSinSignificado =
    [
        TSqlTokenType.WhiteSpace,
        TSqlTokenType.SingleLineComment,
        TSqlTokenType.MultilineComment
    ];

    private static readonly HashSet<TSqlTokenType> TokensDondeNoSeSugiere =
    [
        TSqlTokenType.AsciiStringLiteral,
        TSqlTokenType.UnicodeStringLiteral,
        TSqlTokenType.SingleLineComment,
        TSqlTokenType.MultilineComment
    ];

    public IReadOnlyList<string> PalabrasClave => PalabrasDeTSql.PalabrasClave;

    public IReadOnlyList<string> Funciones => PalabrasDeTSql.Funciones;

    public ContextoDeAutocompletado Analizar(string textoCompleto, int posicionDelCursor)
    {
        var cursor = Math.Clamp(posicionDelCursor, 0, textoCompleto.Length);
        var tokensConFinDeArchivo = new TSql170Parser(initialQuotedIdentifiers: true)
            .GetTokenStream(new StringReader(textoCompleto), out var errores);

        // Con errores léxicos (ej. una comilla sin cerrar) lo más probable es estar escribiendo un texto
        if (tokensConFinDeArchivo is null || errores.Count > 0)
        {
            return ContextoDeAutocompletado.SinSugerencias;
        }

        // El token de fin de archivo no tiene texto y no aporta nada al análisis
        IList<TSqlParserToken> tokens = tokensConFinDeArchivo.Where(token => token.TokenType != TSqlTokenType.EndOfFile).ToList();

        if (EstaDentroDeTextoOComentario(tokens, cursor))
        {
            return ContextoDeAutocompletado.SinSugerencias;
        }

        var palabraParcial = LeerPalabraHaciaAtras(textoCompleto, cursor);
        var inicioDeLaPalabra = cursor - palabraParcial.Length;
        var tablasDelLote = ExtraerTablas(ObtenerTokensDelLote(tokens, cursor));

        if (inicioDeLaPalabra > 0 && textoCompleto[inicioDeLaPalabra - 1] == '.')
        {
            var calificador = QuitarCorchetes(LeerPalabraHaciaAtras(textoCompleto, inicioDeLaPalabra - 1));
            return new ContextoDeAutocompletado(TipoDeContexto.DespuesDePunto, palabraParcial, calificador, tablasDelLote);
        }

        var tipo = EsperaUnNombreDeTabla(tokens, inicioDeLaPalabra) ? TipoDeContexto.NombreDeTabla : TipoDeContexto.General;
        return new ContextoDeAutocompletado(tipo, palabraParcial, Calificador: null, tablasDelLote);
    }

    private static bool EstaDentroDeTextoOComentario(IList<TSqlParserToken> tokens, int cursor) =>
        tokens.Any(token =>
            TokensDondeNoSeSugiere.Contains(token.TokenType)
            && cursor > token.Offset
            && (cursor < token.Offset + token.Text.Length || token.TokenType == TSqlTokenType.SingleLineComment && cursor == token.Offset + token.Text.Length));

    private static bool EsperaUnNombreDeTabla(IList<TSqlParserToken> tokens, int inicioDeLaPalabra)
    {
        var tokenAnterior = tokens.LastOrDefault(token =>
            token.Offset + token.Text.Length <= inicioDeLaPalabra && !TokensSinSignificado.Contains(token.TokenType));

        return tokenAnterior is not null && TokensQueIntroducenUnaTabla.Contains(tokenAnterior.TokenType);
    }

    /// <summary>
    /// Solo cuentan las tablas del lote actual (entre dos GO), para no mezclar alias de otras consultas.
    /// </summary>
    private static List<TSqlParserToken> ObtenerTokensDelLote(IList<TSqlParserToken> tokens, int cursor)
    {
        var inicio = tokens.LastOrDefault(token => token.TokenType == TSqlTokenType.Go && token.Offset < cursor)?.Offset ?? -1;
        var fin = tokens.FirstOrDefault(token => token.TokenType == TSqlTokenType.Go && token.Offset >= cursor)?.Offset ?? int.MaxValue;

        return tokens
            .Where(token => token.Offset > inicio && token.Offset < fin && !TokensSinSignificado.Contains(token.TokenType))
            .ToList();
    }

    /// <summary>
    /// Recorre FROM, JOIN, UPDATE e INTO para encontrar "esquema.tabla alias". En la cláusula FROM
    /// también se leen las tablas separadas por comas (FROM a, b o FROM a JOIN b ON ..., c).
    /// </summary>
    private static List<ReferenciaDeTabla> ExtraerTablas(List<TSqlParserToken> tokens)
    {
        var tablas = new List<ReferenciaDeTabla>();

        for (var indice = 0; indice < tokens.Count; indice++)
        {
            if (!TokensQueIntroducenUnaTabla.Contains(tokens[indice].TokenType))
            {
                continue;
            }

            AgregarReferencia(tablas, tokens, indice + 1);

            if (tokens[indice].TokenType == TSqlTokenType.From)
            {
                foreach (var posicionDeLaComa in BuscarComasDeLaClausulaFrom(tokens, indice + 1))
                {
                    AgregarReferencia(tablas, tokens, posicionDeLaComa + 1);
                }
            }
        }

        return tablas;
    }

    private static void AgregarReferencia(List<ReferenciaDeTabla> tablas, List<TSqlParserToken> tokens, int indice)
    {
        if (LeerReferenciaDeTabla(tokens, ref indice) is { } referencia)
        {
            tablas.Add(referencia);
        }
    }

    /// <summary>
    /// Comas fuera de paréntesis desde el FROM hasta el fin de la cláusula (WHERE, GROUP BY, punto y coma...).
    /// </summary>
    private static IEnumerable<int> BuscarComasDeLaClausulaFrom(List<TSqlParserToken> tokens, int inicio)
    {
        var profundidadDeParentesis = 0;

        for (var indice = inicio; indice < tokens.Count; indice++)
        {
            var tipo = tokens[indice].TokenType;

            if (tipo == TSqlTokenType.LeftParenthesis)
            {
                profundidadDeParentesis++;
            }
            else if (tipo == TSqlTokenType.RightParenthesis && --profundidadDeParentesis < 0)
            {
                yield break;
            }
            else if (profundidadDeParentesis == 0 && TokensQueTerminanLaClausulaFrom.Contains(tipo))
            {
                yield break;
            }
            else if (profundidadDeParentesis == 0 && tipo == TSqlTokenType.Comma)
            {
                yield return indice;
            }
        }
    }

    private static ReferenciaDeTabla? LeerReferenciaDeTabla(List<TSqlParserToken> tokens, ref int indice)
    {
        var partesDelNombre = new List<string>();

        while (indice < tokens.Count && EsIdentificador(tokens[indice]))
        {
            partesDelNombre.Add(QuitarCorchetes(tokens[indice].Text));
            indice++;

            if (indice < tokens.Count && tokens[indice].TokenType == TSqlTokenType.Dot)
            {
                indice++;
                continue;
            }

            break;
        }

        if (partesDelNombre.Count == 0)
        {
            return null;
        }

        if (indice < tokens.Count && tokens[indice].TokenType == TSqlTokenType.As)
        {
            indice++;
        }

        string? alias = null;

        if (indice < tokens.Count && EsIdentificador(tokens[indice]))
        {
            alias = QuitarCorchetes(tokens[indice].Text);
            indice++;
        }

        var esquema = partesDelNombre.Count >= 2 ? partesDelNombre[^2] : null;
        return new ReferenciaDeTabla(esquema, partesDelNombre[^1], alias);
    }

    private static bool EsIdentificador(TSqlParserToken token) =>
        token.TokenType is TSqlTokenType.Identifier or TSqlTokenType.QuotedIdentifier;

    private static string LeerPalabraHaciaAtras(string texto, int posicion)
    {
        var inicio = posicion;

        while (inicio > 0 && EsCaracterDeNombre(texto[inicio - 1]))
        {
            inicio--;
        }

        return texto[inicio..posicion];
    }

    private static bool EsCaracterDeNombre(char caracter) =>
        char.IsLetterOrDigit(caracter) || caracter is '_' or '@' or '#' or '[' or ']';

    private static string QuitarCorchetes(string nombre) => nombre.Trim('[', ']');
}
