using System.Text.RegularExpressions;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Divide un script por GO igual que SSMS y sqlcmd: GO es un separador solo cuando está solo en su línea
/// y fuera de un texto, un identificador entre comillas o un comentario de bloque.
/// </summary>
/// <remarks>
/// No se usa el analizador léxico de ScriptDom porque ante cualquier error léxico (un carácter raro,
/// un comentario sin cerrar) deja de reconocer los GO, y entonces todo el script viajaba como un solo lote:
/// cada CREATE PROCEDURE fallaba por no ser la primera instrucción del lote. Así, un error en un lote
/// solo afecta a ese lote.
/// </remarks>
public sealed partial class DivisorDeLotesTSql : IDivisorDeLotes
{
    private const int RepeticionesPredeterminadas = 1;
    private const int SinComentarioAbierto = 0;

    private static readonly char[] SaltosDeLinea = ['\r', '\n'];

    public IReadOnlyList<LoteSql> DividirEnLotes(string textoSql, int lineaInicialEnElEditor = 1)
    {
        var lotes = new List<LoteSql>();
        var inicioDelLoteActual = 0;
        var lineaDelLoteActual = 1;
        var numeroDeLinea = 1;

        // Lo que sigue abierto al terminar una línea: un texto o identificador (su carácter de cierre)
        // o comentarios de bloque, que en T-SQL se pueden anidar
        char? delimitadorAbierto = null;
        var profundidadDeComentario = SinComentarioAbierto;

        foreach (var (inicio, fin, inicioDeLaSiguiente) in RecorrerLineas(textoSql))
        {
            var linea = textoSql[inicio..fin];
            var estaFueraDeTextoYComentario = delimitadorAbierto is null && profundidadDeComentario == SinComentarioAbierto;

            if (estaFueraDeTextoYComentario && ExpresionDeGo().Match(linea) is { Success: true } go)
            {
                var textoDelLote = textoSql[inicioDelLoteActual..inicio];
                AgregarSiNoEstaVacio(lotes, textoDelLote, lineaInicialEnElEditor + lineaDelLoteActual - 1, LeerRepeticiones(go));

                inicioDelLoteActual = inicioDeLaSiguiente;
                lineaDelLoteActual = numeroDeLinea + 1;
            }
            else
            {
                AvanzarSobreLaLinea(linea, ref delimitadorAbierto, ref profundidadDeComentario);
            }

            numeroDeLinea++;
        }

        var textoRestante = textoSql[inicioDelLoteActual..];
        AgregarSiNoEstaVacio(lotes, textoRestante, lineaInicialEnElEditor + lineaDelLoteActual - 1, RepeticionesPredeterminadas);

        return lotes;
    }

    /// <summary>
    /// Recorre las líneas aceptando cualquier fin de línea (\r\n, \n o \r solo, de archivos antiguos).
    /// </summary>
    private static IEnumerable<(int Inicio, int Fin, int InicioDeLaSiguiente)> RecorrerLineas(string texto)
    {
        var inicio = 0;

        while (inicio < texto.Length)
        {
            var fin = texto.IndexOfAny(SaltosDeLinea, inicio);

            if (fin < 0)
            {
                yield return (inicio, texto.Length, texto.Length);
                yield break;
            }

            var esSaltoDeWindows = texto[fin] == '\r' && fin + 1 < texto.Length && texto[fin + 1] == '\n';
            var inicioDeLaSiguiente = esSaltoDeWindows ? fin + 2 : fin + 1;

            yield return (inicio, fin, inicioDeLaSiguiente);
            inicio = inicioDeLaSiguiente;
        }
    }

    /// <summary>
    /// Actualiza qué queda abierto (texto, identificador o comentario) después de leer una línea.
    /// </summary>
    private static void AvanzarSobreLaLinea(string linea, ref char? delimitadorAbierto, ref int profundidadDeComentario)
    {
        for (var indice = 0; indice < linea.Length; indice++)
        {
            var caracter = linea[indice];
            var siguiente = indice + 1 < linea.Length ? linea[indice + 1] : (char?)null;

            if (profundidadDeComentario > SinComentarioAbierto)
            {
                if (caracter == '*' && siguiente == '/')
                {
                    profundidadDeComentario--;
                    indice++;
                }
                else if (caracter == '/' && siguiente == '*')
                {
                    profundidadDeComentario++;
                    indice++;
                }

                continue;
            }

            if (delimitadorAbierto is { } cierre)
            {
                if (caracter == cierre)
                {
                    // El delimitador duplicado ('' o ]]) es un escape dentro del texto, no lo cierra
                    if (siguiente == cierre)
                    {
                        indice++;
                    }
                    else
                    {
                        delimitadorAbierto = null;
                    }
                }

                continue;
            }

            switch (caracter)
            {
                case '-' when siguiente == '-':
                    return;
                case '/' when siguiente == '*':
                    profundidadDeComentario++;
                    indice++;
                    break;
                case '\'' or '"':
                    delimitadorAbierto = caracter;
                    break;
                case '[':
                    delimitadorAbierto = ']';
                    break;
            }
        }
    }

    /// <summary>
    /// Lee el número opcional que acompaña al GO, como en "GO 5".
    /// </summary>
    private static int LeerRepeticiones(Match go) =>
        int.TryParse(go.Groups["repeticiones"].Value, out var repeticiones) && repeticiones > 0
            ? repeticiones
            : RepeticionesPredeterminadas;

    private static void AgregarSiNoEstaVacio(List<LoteSql> lotes, string texto, int lineaInicial, int repeticiones)
    {
        var lote = new LoteSql(texto, lineaInicial, repeticiones);

        if (!lote.EstaVacio)
        {
            lotes.Add(lote);
        }
    }

    // GO solo en su línea, con repeticiones opcionales ("GO 5"), punto y coma y comentario de línea al final
    [GeneratedRegex(@"^\s*GO(?:\s+(?<repeticiones>\d+))?\s*;?\s*(?:--.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex ExpresionDeGo();
}
