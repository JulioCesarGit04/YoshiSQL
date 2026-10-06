namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Una relación de dependencia de un objeto: otro objeto del que depende o que lo usa.
/// </summary>
public sealed record DependenciaDeObjeto(string? Esquema, string Nombre, string Relacion)
{
    public string NombreCompleto => string.IsNullOrEmpty(Esquema) ? Nombre : $"{Esquema}.{Nombre}";
}
