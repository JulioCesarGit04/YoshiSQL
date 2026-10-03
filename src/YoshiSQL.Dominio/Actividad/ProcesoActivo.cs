namespace YoshiSQL.Dominio.Actividad;

/// <summary>
/// Una sesión conectada al servidor y, si está ejecutando algo, qué hace y si está bloqueada.
/// </summary>
/// <param name="BloqueadoPorSesion">Sesión que la está bloqueando; nulo si no hay bloqueo.</param>
public sealed record ProcesoActivo(
    int IdDeSesion,
    string InicioDeSesion,
    string Equipo,
    string Programa,
    string BaseDeDatos,
    string Estado,
    string? Comando,
    string? TipoDeEspera,
    int? BloqueadoPorSesion,
    long TiempoTranscurridoEnMilisegundos,
    long TiempoDeCpuEnMilisegundos,
    long Lecturas,
    string? TextoSql)
{
    public bool EstaBloqueado => BloqueadoPorSesion is > 0;
}
