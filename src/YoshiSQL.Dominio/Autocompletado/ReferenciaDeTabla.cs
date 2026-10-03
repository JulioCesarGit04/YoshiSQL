namespace YoshiSQL.Dominio.Autocompletado;

/// <summary>
/// Tabla mencionada en la instrucción actual, ej. "dbo.Clientes c" o "Pedidos".
/// </summary>
public sealed record ReferenciaDeTabla(string? Esquema, string Nombre, string? Alias)
{
    public bool CoincideCon(string calificador) =>
        string.Equals(Alias, calificador, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Nombre, calificador, StringComparison.OrdinalIgnoreCase);
}
