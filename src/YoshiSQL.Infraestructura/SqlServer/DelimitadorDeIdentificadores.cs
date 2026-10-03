namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Escribe nombres y textos de forma segura dentro del código T-SQL generado.
/// </summary>
internal static class DelimitadorDeIdentificadores
{
    /// <summary>Clientes -> [Clientes]; Mi]Tabla -> [Mi]]Tabla]</summary>
    public static string Delimitar(string identificador) =>
        $"[{identificador.Replace("]", "]]", StringComparison.Ordinal)}]";

    public static string Delimitar(string esquema, string nombre) =>
        $"{Delimitar(esquema)}.{Delimitar(nombre)}";
}
