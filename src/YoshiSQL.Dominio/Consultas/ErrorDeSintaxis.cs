namespace YoshiSQL.Dominio.Consultas;

/// <summary>
/// Un error de sintaxis encontrado al analizar el código, antes de ejecutarlo.
/// </summary>
/// <param name="Posicion">Posición del carácter dentro del texto analizado, empezando en 0.</param>
public sealed record ErrorDeSintaxis(int Posicion, int Linea, int Columna, string Mensaje);
