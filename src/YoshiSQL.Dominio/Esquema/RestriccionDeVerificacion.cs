namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Regla que deben cumplir las filas de una tabla (restricción CHECK).
/// </summary>
/// <param name="Nombre">Nulo si SQL Server le puso el nombre.</param>
/// <param name="Definicion">Condición tal como la guarda el catálogo, ej. "([Precio]&gt;=(0))".</param>
public sealed record RestriccionDeVerificacion(Tabla Tabla, string? Nombre, string Definicion, bool EstaDeshabilitada);
