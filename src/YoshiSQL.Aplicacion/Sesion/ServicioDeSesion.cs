using YoshiSQL.Aplicacion.Conexiones;
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
    private readonly ServicioDeConexiones _servicioDeConexiones;

    public ServicioDeSesion(
        IRepositorioDeSesion repositorioDeSesion,
        IRepositorioDeConexiones repositorioDeConexiones,
        ServicioDeConexiones servicioDeConexiones)
    {
        _repositorioDeSesion = repositorioDeSesion;
        _repositorioDeConexiones = repositorioDeConexiones;
        _servicioDeConexiones = servicioDeConexiones;
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

    /// <summary>
    /// Intenta reconectar con la contraseña guardada, sin preguntarle nada al usuario.
    /// </summary>
    /// <returns>El servidor conectado, o nulo si no hay contraseña guardada o el servidor no responde.</returns>
    public async Task<ServidorConectado?> ReconectarSinPreguntarAsync(PerfilDeConexion perfil, CancellationToken tokenDeCancelacion)
    {
        var contrasena = await _servicioDeConexiones.ObtenerContrasenaGuardadaAsync(perfil, tokenDeCancelacion);

        if (contrasena is null && perfil.TipoDeAutenticacion == TipoDeAutenticacion.SqlServer)
        {
            return null;
        }

        return await _servicioDeConexiones.ConectarAsync(perfil, contrasena ?? string.Empty, tokenDeCancelacion);
    }

    public Task<string> GuardarScriptsRecuperadosAsync(IReadOnlyList<PestanaGuardada> pestanas, CancellationToken tokenDeCancelacion) =>
        _repositorioDeSesion.GuardarScriptsRecuperadosAsync(pestanas, tokenDeCancelacion);
}
