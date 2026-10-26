using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Sesion;

namespace YoshiSQL.Aplicacion.Sesion;

/// <summary>
/// Restaura las pestañas de la última vez y protege los scripts sin guardar ante un cierre inesperado.
/// </summary>
public sealed class ServicioDeSesion
{
    private readonly IRepositorioDeSesion _repositorioDeSesion;
    private readonly IRepositorioDeConexiones _repositorioDeConexiones;

    public ServicioDeSesion(
        IRepositorioDeSesion repositorioDeSesion,
        IRepositorioDeConexiones repositorioDeConexiones)
    {
        _repositorioDeSesion = repositorioDeSesion;
        _repositorioDeConexiones = repositorioDeConexiones;
    }

    public async Task<SesionAnterior> IniciarAsync(CancellationToken tokenDeCancelacion)
    {
        var estado = await _repositorioDeSesion.CargarAsync(tokenDeCancelacion);
        var seCerroIncorrectamente = await _repositorioDeSesion.RegistrarInicioAsync(tokenDeCancelacion);
        return new SesionAnterior(estado, seCerroIncorrectamente);
    }

    public Task GuardarAsync(EstadoDeLaSesion estado, CancellationToken tokenDeCancelacion) =>
        _repositorioDeSesion.GuardarAsync(estado, tokenDeCancelacion);

    public async Task CerrarCorrectamenteAsync(EstadoDeLaSesion estadoFinal, CancellationToken tokenDeCancelacion)
    {
        await _repositorioDeSesion.GuardarAsync(estadoFinal, tokenDeCancelacion);
        await _repositorioDeSesion.RegistrarCierreCorrectoAsync(tokenDeCancelacion);
    }

    public async Task<PerfilDeConexion?> BuscarPerfilAsync(Guid idDelPerfil, CancellationToken tokenDeCancelacion)
    {
        var perfiles = await _repositorioDeConexiones.ObtenerTodosAsync(tokenDeCancelacion);
        return perfiles.FirstOrDefault(perfil => perfil.Id == idDelPerfil);
    }

    public Task<string> GuardarScriptsRecuperadosAsync(IReadOnlyList<PestanaGuardada> pestanas, CancellationToken tokenDeCancelacion) =>
        _repositorioDeSesion.GuardarScriptsRecuperadosAsync(pestanas, tokenDeCancelacion);
}
