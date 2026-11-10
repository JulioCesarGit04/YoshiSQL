namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Nombre alternativo de otro objeto (CREATE SYNONYM).
/// </summary>
/// <param name="ObjetoBase">Objeto al que apunta, tal como lo guarda el catálogo, ej. "[OtraBase].[dbo].[Clientes]".</param>
public sealed record Sinonimo(string Esquema, string Nombre, string ObjetoBase);
