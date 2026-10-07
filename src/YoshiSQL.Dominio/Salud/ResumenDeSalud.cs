namespace YoshiSQL.Dominio.Salud;

/// <summary>Cifras generales de una base de datos para el panel de salud.</summary>
public sealed record ResumenDeSalud(int Tablas, long Filas, long TamanoKb, long EspacioUsadoKb);
