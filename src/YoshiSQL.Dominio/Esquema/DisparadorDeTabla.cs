namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Disparador (trigger) de una tabla o vista junto con su código.
/// </summary>
/// <param name="Definicion">Nulo si está cifrado o no hay permiso para verlo.</param>
public sealed record DisparadorDeTabla(
    string Esquema,
    string Nombre,
    string EsquemaDelObjeto,
    string NombreDelObjeto,
    string? Definicion,
    bool EstaDeshabilitado)
{
    public string NombreCompleto => $"{Esquema}.{Nombre}";
}
