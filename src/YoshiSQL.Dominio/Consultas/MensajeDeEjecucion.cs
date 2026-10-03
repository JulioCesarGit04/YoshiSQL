namespace YoshiSQL.Dominio.Consultas;

/// <summary>
/// Mensaje de la pestaña "Mensajes": filas afectadas, PRINT, avisos o errores.
/// </summary>
/// <param name="NumeroDeLinea">Línea del editor relacionada con el mensaje, si se conoce.</param>
public sealed record MensajeDeEjecucion(TipoDeMensaje Tipo, string Texto, int? NumeroDeLinea = null)
{
    public static MensajeDeEjecucion Informacion(string texto) => new(TipoDeMensaje.Informacion, texto);

    public static MensajeDeEjecucion Error(string texto, int? numeroDeLinea = null) =>
        new(TipoDeMensaje.Error, texto, numeroDeLinea);
}
