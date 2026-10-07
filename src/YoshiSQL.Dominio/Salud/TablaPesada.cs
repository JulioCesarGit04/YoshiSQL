namespace YoshiSQL.Dominio.Salud;

/// <summary>Una tabla con su cantidad de filas y el espacio que ocupa.</summary>
public sealed record TablaPesada(string Nombre, long Filas, long EspacioKb);
