namespace YoshiSQL.Aplicacion.Errores;

/// <summary>
/// Qué estaba haciendo el usuario cuando ocurrió el error, para que el registro sea útil.
/// </summary>
/// <param name="Accion">Acción en palabras del usuario, ej. "Abrir diagrama".</param>
public sealed record ContextoDeError(string Accion, string? Servidor = null, string? BaseDeDatos = null);
