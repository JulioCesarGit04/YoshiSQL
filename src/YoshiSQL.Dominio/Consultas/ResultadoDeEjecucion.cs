namespace YoshiSQL.Dominio.Consultas;

/// <summary>
/// Todo lo que produjo la ejecución de un script: resultados, mensajes y tiempo.
/// </summary>
public sealed record ResultadoDeEjecucion(
    EstadoDeEjecucion Estado,
    IReadOnlyList<ConjuntoDeResultados> ConjuntosDeResultados,
    IReadOnlyList<MensajeDeEjecucion> Mensajes,
    TimeSpan Duracion,
    string BaseDeDatosAlTerminar)
{
    public int TotalDeFilas => ConjuntosDeResultados.Sum(conjunto => conjunto.CantidadDeFilas);

    public bool TieneErrores => Mensajes.Any(mensaje => mensaje.Tipo == TipoDeMensaje.Error);
}
