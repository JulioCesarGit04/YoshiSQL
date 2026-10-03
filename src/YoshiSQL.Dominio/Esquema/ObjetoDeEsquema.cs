namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Objeto que pertenece a un esquema dentro de una base de datos (ej. dbo.Clientes).
/// </summary>
public abstract record ObjetoDeEsquema(string Esquema, string Nombre)
{
    public abstract TipoDeObjeto Tipo { get; }

    public string NombreCompleto => $"{Esquema}.{Nombre}";
}
