namespace YoshiSQL.Aplicacion.Consultas;

/// <summary>
/// Código a ejecutar: todo el editor o solo el texto subrayado.
/// </summary>
/// <param name="LineaInicial">Línea del editor donde empieza el fragmento.</param>
public sealed record FragmentoDeCodigo(string Texto, int LineaInicial);
