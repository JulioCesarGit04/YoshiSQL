using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Aplicacion.Preferencias;

public sealed class ServicioDePreferencias
{
    private readonly IRepositorioDePreferencias _repositorio;

    public ServicioDePreferencias(IRepositorioDePreferencias repositorio)
    {
        _repositorio = repositorio;
    }

    public PreferenciasDelUsuario Actuales { get; private set; } = PreferenciasDelUsuario.Predeterminadas;

    public event EventHandler<PreferenciasDelUsuario>? PreferenciasCambiadas;

    public async Task CargarAsync(CancellationToken tokenDeCancelacion)
    {
        Actuales = await _repositorio.CargarAsync(tokenDeCancelacion);
        PreferenciasCambiadas?.Invoke(this, Actuales);
    }

    public async Task GuardarAsync(PreferenciasDelUsuario preferencias, CancellationToken tokenDeCancelacion)
    {
        var preferenciasNormalizadas = preferencias.Normalizar();
        await _repositorio.GuardarAsync(preferenciasNormalizadas, tokenDeCancelacion);
        Actuales = preferenciasNormalizadas;
        PreferenciasCambiadas?.Invoke(this, Actuales);
    }
}
