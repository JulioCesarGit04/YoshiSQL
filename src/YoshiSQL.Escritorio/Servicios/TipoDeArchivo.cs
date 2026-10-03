namespace YoshiSQL.Escritorio.Servicios;

/// <summary>
/// Tipo de archivo que se ofrece en la ventana de "Guardar como".
/// </summary>
/// <param name="Extension">Extensión sin punto, ej. "sql".</param>
public sealed record TipoDeArchivo(string Descripcion, string Extension)
{
    public static readonly TipoDeArchivo ScriptSql = new("Scripts SQL", "sql");
}
