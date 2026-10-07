namespace YoshiSQL.Dominio.Salud;

/// <summary>Un índice con su porcentaje de fragmentación.</summary>
public sealed record IndiceFragmentado(string Tabla, string Indice, double Fragmentacion, long Paginas);
