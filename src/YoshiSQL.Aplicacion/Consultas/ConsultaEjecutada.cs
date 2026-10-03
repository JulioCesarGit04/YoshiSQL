using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Aplicacion.Consultas;

public sealed record ConsultaEjecutada(
    string Texto,
    string BaseDeDatos,
    DateTimeOffset Momento,
    EstadoDeEjecucion Estado,
    TimeSpan Duracion);
