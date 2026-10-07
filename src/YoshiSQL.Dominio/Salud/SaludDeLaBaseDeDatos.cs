namespace YoshiSQL.Dominio.Salud;

/// <summary>Chequeo general de una base de datos: resumen, tablas más pesadas e índices fragmentados.</summary>
public sealed record SaludDeLaBaseDeDatos(
    ResumenDeSalud Resumen,
    IReadOnlyList<TablaPesada> TablasPesadas,
    IReadOnlyList<IndiceFragmentado> IndicesFragmentados);
