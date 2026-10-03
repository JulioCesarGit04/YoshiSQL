using YoshiSQL.Dominio.Autocompletado;

namespace YoshiSQL.Dominio.Contratos;

/// <summary>
/// Entiende la sintaxis del motor lo suficiente para saber qué sugerir en la posición del cursor.
/// </summary>
public interface IAnalizadorDeContextoSql
{
    IReadOnlyList<string> PalabrasClave { get; }

    IReadOnlyList<string> Funciones { get; }

    ContextoDeAutocompletado Analizar(string textoCompleto, int posicionDelCursor);
}
