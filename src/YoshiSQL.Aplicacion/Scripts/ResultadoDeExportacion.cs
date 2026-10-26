namespace YoshiSQL.Aplicacion.Scripts;

/// <param name="Advertencias">
/// Lo que no se pudo exportar (datos ilegibles, objetos cifrados). También queda como comentario en el script,
/// pero se informa al terminar para que nadie dé por buena una exportación incompleta.
/// </param>
public sealed record ResultadoDeExportacion(string Script, IReadOnlyList<string> Advertencias);
