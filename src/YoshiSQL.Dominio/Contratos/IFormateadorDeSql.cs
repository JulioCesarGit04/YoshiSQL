namespace YoshiSQL.Dominio.Contratos;

public interface IFormateadorDeSql
{
    /// <summary>
    /// Devuelve el código ordenado y con palabras clave en mayúsculas.
    /// Lanza ErrorDeYoshiSql si el código no se puede formatear sin perder información.
    /// </summary>
    string Formatear(string textoSql);
}
