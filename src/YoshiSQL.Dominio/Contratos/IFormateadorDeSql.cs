using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Dominio.Contratos;

public interface IFormateadorDeSql
{
    /// <summary>
    /// Devuelve el código ordenado y con palabras clave en mayúsculas.
    /// Lanza ErrorDeYoshiSql si el código no se puede formatear sin perder información.
    /// </summary>
    string Formatear(string textoSql);

    /// <summary>
    /// Analiza el código sin ejecutarlo y devuelve sus errores de sintaxis (vacío si no tiene).
    /// No valida que existan las tablas o columnas: eso solo lo sabe el servidor.
    /// </summary>
    IReadOnlyList<ErrorDeSintaxis> BuscarErroresDeSintaxis(string textoSql);
}
