using Microsoft.SqlServer.TransactSql.ScriptDom;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Divide un script por GO usando el analizador léxico oficial de T-SQL, de modo que
/// un "GO" dentro de un texto, un comentario o un nombre de columna no se confunda con el separador.
/// </summary>
public sealed class DivisorDeLotesTSql : IDivisorDeLotes
{
    private const int RepeticionesPredeterminadas = 1;

    public IReadOnlyList<LoteSql> DividirEnLotes(string textoSql, int lineaInicialEnElEditor = 1)
    {
        var analizador = new TSql170Parser(initialQuotedIdentifiers: true);
        var tokens = analizador.GetTokenStream(new StringReader(textoSql), out var erroresLexicos);

        // Con errores léxicos (ej. una comilla sin cerrar) se envía todo junto y el servidor informa el error
        if (tokens is null || erroresLexicos.Count > 0)
        {
            return CrearListaConUnSoloLote(textoSql, lineaInicialEnElEditor);
        }

        var lotes = new List<LoteSql>();
        var inicioDelLoteActual = 0;
        var lineaDelLoteActual = 1;

        for (var indice = 0; indice < tokens.Count; indice++)
        {
            var token = tokens[indice];

            if (token.TokenType != TSqlTokenType.Go)
            {
                continue;
            }

            var textoDelLote = textoSql[inicioDelLoteActual..token.Offset];
            var repeticiones = LeerRepeticionesDelGo(tokens, indice);
            AgregarSiNoEstaVacio(lotes, textoDelLote, lineaInicialEnElEditor + lineaDelLoteActual - 1, repeticiones);

            inicioDelLoteActual = BuscarInicioDeLaSiguienteLinea(textoSql, token.Offset);
            lineaDelLoteActual = token.Line + 1;
        }

        var textoRestante = textoSql[inicioDelLoteActual..];
        AgregarSiNoEstaVacio(lotes, textoRestante, lineaInicialEnElEditor + lineaDelLoteActual - 1, RepeticionesPredeterminadas);

        return lotes;
    }

    /// <summary>
    /// Lee el número opcional que acompaña al GO en la misma línea, como en "GO 5".
    /// </summary>
    private static int LeerRepeticionesDelGo(IList<TSqlParserToken> tokens, int indiceDelGo)
    {
        var lineaDelGo = tokens[indiceDelGo].Line;

        for (var indice = indiceDelGo + 1; indice < tokens.Count && tokens[indice].Line == lineaDelGo; indice++)
        {
            var token = tokens[indice];

            if (token.TokenType == TSqlTokenType.WhiteSpace)
            {
                continue;
            }

            if (token.TokenType == TSqlTokenType.Integer && int.TryParse(token.Text, out var repeticiones) && repeticiones > 0)
            {
                return repeticiones;
            }

            break;
        }

        return RepeticionesPredeterminadas;
    }

    private static int BuscarInicioDeLaSiguienteLinea(string textoSql, int posicionActual)
    {
        var posicionDelSaltoDeLinea = textoSql.IndexOf('\n', posicionActual);
        return posicionDelSaltoDeLinea < 0 ? textoSql.Length : posicionDelSaltoDeLinea + 1;
    }

    private static void AgregarSiNoEstaVacio(List<LoteSql> lotes, string texto, int lineaInicial, int repeticiones)
    {
        var lote = new LoteSql(texto, lineaInicial, repeticiones);

        if (!lote.EstaVacio)
        {
            lotes.Add(lote);
        }
    }

    private static IReadOnlyList<LoteSql> CrearListaConUnSoloLote(string textoSql, int lineaInicial)
    {
        var lote = new LoteSql(textoSql, lineaInicial);
        return lote.EstaVacio ? [] : [lote];
    }
}
