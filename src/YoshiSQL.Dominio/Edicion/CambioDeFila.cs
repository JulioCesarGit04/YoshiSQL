namespace YoshiSQL.Dominio.Edicion;

/// <summary>
/// Un cambio pendiente en la grilla de edición.
/// </summary>
/// <param name="ValoresOriginales">Valores leídos de la base (para ubicar la fila por su llave primaria); vacío en filas nuevas.</param>
/// <param name="ValoresNuevos">Valores escritos por el usuario, en el mismo orden de columnas; nulo significa NULL.</param>
public sealed record CambioDeFila(EstadoDeFila Tipo, IReadOnlyList<object?> ValoresOriginales, IReadOnlyList<object?> ValoresNuevos);
