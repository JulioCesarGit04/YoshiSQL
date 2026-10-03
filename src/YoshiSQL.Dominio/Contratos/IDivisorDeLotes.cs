using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Dominio.Contratos;

public interface IDivisorDeLotes
{
    /// <summary>
    /// Separa el texto en lotes usando el separador GO.
    /// </summary>
    /// <param name="lineaInicialEnElEditor">
    /// Línea donde comienza el texto; sirve cuando solo se ejecuta una selección.
    /// </param>
    IReadOnlyList<LoteSql> DividirEnLotes(string textoSql, int lineaInicialEnElEditor = 1);
}
