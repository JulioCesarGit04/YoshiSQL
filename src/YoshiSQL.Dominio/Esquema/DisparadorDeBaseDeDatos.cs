namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Disparador de toda la base de datos (DDL): se ejecuta ante un CREATE, ALTER o DROP, por ejemplo para auditar cambios.
/// </summary>
/// <param name="Definicion">Nulo si está cifrado o no hay permiso para verlo.</param>
public sealed record DisparadorDeBaseDeDatos(string Nombre, string? Definicion, bool EstaDeshabilitado);
