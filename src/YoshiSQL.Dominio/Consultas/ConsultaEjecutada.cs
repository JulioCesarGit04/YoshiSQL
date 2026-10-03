namespace YoshiSQL.Dominio.Consultas;

/// <summary>
/// Una entrada del historial: qué se ejecutó, dónde, cuándo y cómo terminó.
/// </summary>
public sealed record ConsultaEjecutada(
    string Texto,
    string Servidor,
    string BaseDeDatos,
    DateTimeOffset Momento,
    EstadoDeEjecucion Estado,
    TimeSpan Duracion)
{
    public bool TerminoConErrores => Estado is EstadoDeEjecucion.CompletadaConErrores or EstadoDeEjecucion.Fallida;
}
